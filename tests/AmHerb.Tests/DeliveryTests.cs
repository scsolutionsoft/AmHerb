using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Tests;

public class AddressTests
{
    [Fact] public void Address_data_and_hierarchy_are_validated()
    {
        Assert.Equal(77, ThaiAddresses.Children(null).Count());
        var input = new CheckoutInput { HouseNumber = "99/1", SubdistrictCode = "100101", PostalCode = "10200" };
        Assert.Contains("กรุงเทพมหานคร", ThaiAddresses.Format(input));
        input.VillageCode = "20100101";
        Assert.Throws<BusinessException>(() => ThaiAddresses.Format(input));
        input.VillageCode = null; input.PostalCode = "99999";
        Assert.Throws<BusinessException>(() => ThaiAddresses.Format(input));
        input.PostalCode = "10200"; input.HouseNumber = " ";
        Assert.Throws<BusinessException>(() => ThaiAddresses.Format(input));
    }
    [Theory]
    [InlineData(" J & T Express ", "JT")]
    [InlineData("เคอร์รี่", "KEX")]
    [InlineData("flash express", "FLASH")]
    [InlineData("เฟลช", "FLASH")]
    [InlineData("KEX Express", "KEX")]
    [InlineData("ไปรษณีย์ไทย", "THAILANDPOST")]
    [InlineData("My Courier", "MYCOURIER")]
    public void Carrier_names_have_canonical_keys(string input, string expected) => Assert.Equal(expected, DeliveryService.Normalize(input));
}

[Collection("SQL")]
public class DeliveryTests(SqlFixture fixture)
{
    private static CheckoutInput Input(long carrier = 1) => new() { CustomerName = "Receiver", Phone = "0812345678", HouseNumber = "99/1", SubdistrictCode = "100101", PostalCode = "10200", ShippingProviderId = carrier };
    [SqlFact] public async Task Online_sale_requires_address_carrier_and_slip_before_confirmation_and_shipping()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var owner = await h.MemberAsync();
        var stores = new MemberStoreService(h.Db, h.Members, new AuditService(h.Db, new Actor(new HttpContextAccessor())));
        var store = await stores.CreateAsync(owner.UserId);
        await stores.SaveAsync(owner.UserId, "Delivery shop", "", "0812345678", true);
        await stores.SetProductAsync(owner.UserId, h.SkuId, true);
        await h.Inventory.ReceiveAsync(h.SkuId, store.WarehouseId, "DELIVERY", h.Clock.Now.AddDays(-1), h.Clock.Now.AddYears(1), 10, 100, "Test");
        var delivery = new DeliveryService(h.Db);
        await delivery.Set(store.Id, 1, false);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(Input(), [new(h.SkuId, 1)], null, null, storeId: store.Id));
        h.Db.ChangeTracker.Clear();
        var bad = Input(2); bad.SubdistrictCode = "missing";
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.CheckoutAsync(bad, [new(h.SkuId, 1)], null, null, storeId: store.Id));
        h.Db.ChangeTracker.Clear();
        var order = await h.Commerce.CheckoutAsync(Input(2), [new(h.SkuId, 1)], null, null, storeId: store.Id);
        Assert.True(order.SlipRequired); Assert.Equal("Kerry / KEX", order.SelectedCarrier);
        Assert.Contains("99/1", order.Address); Assert.Contains("10200", order.Address);
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.PrepareAsync(order.Id));
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.FulfillAsync(order.Id, "KEX", "TRACK", false, "Test"));
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.ConfirmPaymentAsync(order.Id, "BANK", order.CashPayable, "No slip"));
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.SubmitPaymentAsync(order.Id, "BANK"));
        await Assert.ThrowsAsync<BusinessException>(() => h.Commerce.SubmitPaymentAsync(order.Id, "BANK", new byte[40]));
        await h.Commerce.SubmitPaymentAsync(order.Id, "BANK", CheckoutTestData.Slip);
        await h.Commerce.SubmitPaymentAsync(order.Id, "BANK", CheckoutTestData.Slip);
        Assert.Equal(1, await h.Db.PaymentSlips.CountAsync(x => x.OrderId == order.Id));
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        await h.Commerce.ConfirmPaymentAsync(order.Id, "BANK", order.CashPayable, "Verified");
        await h.Commerce.PrepareAsync(order.Id); Assert.Equal(OrderStatus.Processing, order.Status);
        await h.Commerce.FulfillAsync(order.Id, "KEX", "TRACK", false, "Sent");
        await h.Commerce.FulfillAsync(order.Id, "", "", true, "Delivered");
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }
    [SqlFact] public async Task Store_carrier_settings_are_isolated_and_duplicates_are_rejected()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var owner = await h.MemberAsync(); var other = await h.MemberAsync();
        var stores = new MemberStoreService(h.Db, h.Members, new AuditService(h.Db, new Actor(new HttpContextAccessor())));
        var first = await stores.CreateAsync(owner.UserId); var second = await stores.CreateAsync(other.UserId);
        var delivery = new DeliveryService(h.Db);
        await delivery.Set(first.Id, 3, false);
        Assert.False((await delivery.Options(first.Id)).Single(x => x.Id == 3).Enabled);
        Assert.True((await delivery.Options(second.Id)).Single(x => x.Id == 3).Enabled);
        await Assert.ThrowsAsync<BusinessException>(() => delivery.Add(first.Id, " Flash Express "));
        var name = "Local Delivery " + Guid.NewGuid().ToString("N"); await delivery.Add(first.Id, name);
        Assert.True((await delivery.Options(first.Id)).Single(x => x.Name == name).Enabled);
        Assert.False((await delivery.Options(second.Id)).Single(x => x.Name == name).Enabled);
        await Assert.ThrowsAsync<BusinessException>(() => delivery.Add(second.Id, name.ToLowerInvariant()));
    }
    [SqlFact] public async Task Pos_transfer_can_save_slip_atomically_with_order()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var member = await h.MemberAsync(); var session = await h.OpenAsync(member);
        var order = await h.Commerce.CheckoutAsync(new() { CustomerName = "POS", Phone = "0812345678" }, [new(h.SkuId, 1)], null, member.Id, SalesChannel.POS, member.UserId, session.Id, paymentMethod: "ManualBankTransfer", paymentSlip: CheckoutTestData.Slip, slipReference: "POS-BANK");
        Assert.Equal("Submitted", (await h.Db.Payments.SingleAsync(x => x.OrderId == order.Id)).Status);
        Assert.Equal(1, await h.Db.PaymentSlips.CountAsync(x => x.OrderId == order.Id));
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }
}
