using System.Text;
using AmHerb.Web.Controllers;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AmHerb.Tests;

public class DashboardMathTests
{
    [Fact] public void Date_range_uses_Bangkok_midnight_and_equal_previous_window()
    {
        var period = DashboardPeriod.Create(new DateTime(2026, 9, 1), new DateTime(2026, 9, 7), new DateTime(2026, 10, 6));
        Assert.Equal(7, period.Days);
        Assert.Equal(new DateTime(2026, 8, 31, 17, 0, 0, DateTimeKind.Utc), period.StartUtc);
        Assert.Equal(new DateTime(2026, 9, 7, 17, 0, 0, DateTimeKind.Utc), period.EndUtc);
        var defaultPeriod = DashboardPeriod.Create(null, null, new DateTime(2026, 10, 5, 18, 0, 0));
        Assert.Equal(new DateTime(2026, 10, 6), defaultPeriod.To);
        Assert.Equal(30, defaultPeriod.Days);
    }
    [Fact] public void Invalid_ranges_and_future_dates_are_rejected()
    {
        var now = new DateTime(2026, 10, 6);
        Assert.Throws<BusinessException>(() => DashboardPeriod.Create(now, now.AddDays(-1), now));
        Assert.Throws<BusinessException>(() => DashboardPeriod.Create(now.AddDays(-366), now, now));
        Assert.Throws<BusinessException>(() => DashboardPeriod.Create(null, now.AddDays(1), now));
    }
    [Fact] public void Personal_sales_exclude_purchases_outsiders_and_drafts_and_do_not_double_count()
    {
        var own = new Member { UserId = "own", Status = MemberStatus.Active };
        var other = new Member { UserId = "other", Status = MemberStatus.Active };
        var orders = new[] {
            new Order { Id = 1, CashierUserId = "own", Seller = own, Status = OrderStatus.Paid },
            new Order { Id = 2, Seller = own, Status = OrderStatus.Completed },
            new Order { Id = 3, Buyer = own, CashierUserId = "other", Seller = other, Status = OrderStatus.Paid },
            new Order { Id = 4, Seller = own, Status = OrderStatus.Draft },
            new Order { Id = 5, Seller = own, Status = OrderStatus.PendingPayment },
            new Order { Id = 6, Seller = own, Status = OrderStatus.Cancelled }
        }.AsQueryable();
        Assert.Equal(new long[] { 1, 2 }, SalesDashboardService.Sales(SalesDashboardService.ScopeOrders(orders, "own")).Select(x => x.Id));
        Assert.Equal(new long[] { 1, 2, 3 }, SalesDashboardService.Sales(SalesDashboardService.ScopeOrders(orders, null)).Select(x => x.Id));
    }
    [Theory]
    [InlineData("=SUM(A1)")][InlineData("  +cmd")][InlineData("@formula")][InlineData("\t=cmd")]
    public void Csv_export_neutralizes_spreadsheet_formulas(string input) => Assert.StartsWith("\"'", DashboardController.CsvCell(input));
    [Fact] public void Zero_previous_sales_are_not_reported_as_infinite_growth()
    {
        Assert.Null(DashboardSummary.Growth(100, 0));
        Assert.Equal(50m, DashboardSummary.Growth(150, 100));
        Assert.Equal(-100m, DashboardSummary.Growth(0, 100));
        Assert.Equal(0m, DashboardSummary.From([]).Average);
    }
    [Fact] public void Global_reports_and_global_export_require_reports_policy()
    {
        foreach (var method in new[] { nameof(DashboardController.Overview), nameof(DashboardController.ExportOverview) })
            Assert.Equal("Reports", Assert.Single(typeof(DashboardController).GetMethod(method)!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Policy);
        Assert.Single(typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }
}

[Collection("SQL")]
public class DashboardSqlTests(SqlFixture fixture)
{
    [SqlFact] public async Task Real_SQL_dashboard_scopes_sales_referrals_stock_and_export_and_accounts_for_returns()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        h.Clock.Now = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var owner = await h.MemberAsync(); var outsider = await h.MemberAsync();
        var warehouse = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id);
        await h.Inventory.ReceiveAsync(h.SkuId, warehouse.Id, "DASHBOARD-PRIVATE", h.Clock.Now.AddDays(-5), h.Clock.Now.AddDays(25), 5, 100, "Dashboard test");
        var batch = await h.Db.InventoryBatches.SingleAsync(x => x.WarehouseId == warehouse.Id);
        Order Sale(Member seller, DateTime created, OrderStatus status, decimal unitPrice, int returned = 0) => new()
        {
            Number = "DASH-" + Guid.NewGuid().ToString("N"), IdempotencyKey = Guid.NewGuid().ToString("N"),
            SellerMemberId = seller.Id, CashierUserId = seller.UserId, BuyerMemberId = owner.Id,
            CreatedAt = created, Status = status, Channel = SalesChannel.POS, Campaign = seller == owner ? "OWNER-CAMPAIGN" : "OUTSIDER-CAMPAIGN",
            Merchandise = unitPrice * 2, Discount = 20,
            Items = [new OrderItem { SkuId = h.SkuId, Name = "Test", Quantity = 2, ReturnedQuantity = returned, UnitPrice = unitPrice, Discount = 20 }]
        };
        var current = Sale(owner, h.Clock.Now, OrderStatus.Paid, 100, 1);
        var prior = Sale(owner, h.Clock.Now.AddDays(-1), OrderStatus.Paid, 60);
        var unrelated = Sale(outsider, h.Clock.Now, OrderStatus.Paid, 500);
        current.Items[0].Allocations.Add(new StockAllocation { InventoryBatchId = batch.Id, Quantity = 2, ReturnedQuantity = 1 });
        unrelated.Items[0].Allocations.Add(new StockAllocation { InventoryBatchId = batch.Id, Quantity = 2, ReturnedQuantity = 1 });
        h.Db.Orders.AddRange(current, prior, unrelated, Sale(owner, h.Clock.Now, OrderStatus.Draft, 999), Sale(owner, h.Clock.Now, OrderStatus.PendingPayment, 999));
        await h.Db.SaveChangesAsync();
        // Only the restocked return reverses cost; a damaged return remains an expense.
        h.Db.InventoryTransactions.Add(new InventoryTransaction { InventoryBatchId = batch.Id, OrderId = current.Id, Kind = InventoryKind.Return, Quantity = 1 });
        h.Db.ReferralVisits.AddRange(new ReferralVisit { MemberId = owner.Id, ConvertedOrderId = current.Id, CreatedAt = h.Clock.Now }, new ReferralVisit { MemberId = outsider.Id, ConvertedOrderId = unrelated.Id, CreatedAt = h.Clock.Now });
        await h.Db.SaveChangesAsync();
        var service = new SalesDashboardService(h.Db, h.Clock);
        var model = await service.BuildAsync(owner.UserId, h.Clock.Now.Date, h.Clock.Now.Date, SalesChannel.POS);
        Assert.Equal(90m, model.Current.Net); Assert.Equal(1, model.Current.Orders); Assert.Equal(1, model.Current.Units);
        Assert.Equal(100m, model.Previous.Net); Assert.Equal(1, model.Visits); Assert.Equal(1, model.ConvertedVisits);
        Assert.Equal(1, model.PendingOrders); Assert.Equal(0m, model.Current.Cost);
        Assert.Equal("OWNER-CAMPAIGN", Assert.Single(model.Campaigns).Label);
        Assert.Equal(5, Assert.Single(model.Stock).Available); Assert.Equal(5, model.Stock[0].Expiring);
        Assert.Equal(90m, Assert.Single(model.Products).Net); Assert.Equal(100m, Assert.Single(model.Days).Previous);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.UserId)], "Test")) };
        var controller = new DashboardController(service, new Actor(new HttpContextAccessor { HttpContext = context }), null!);
        var csv = Assert.IsType<FileContentResult>(await controller.ExportMine(h.Clock.Now.Date, h.Clock.Now.Date, SalesChannel.POS));
        var contents = Encoding.UTF8.GetString(csv.FileContents);
        Assert.Contains("OWNER-CAMPAIGN", contents); Assert.DoesNotContain("OUTSIDER-CAMPAIGN", contents); Assert.DoesNotContain("ต้นทุนสินค้าสุทธิ", contents);
        var global = await service.BuildAsync(null, h.Clock.Now.Date, h.Clock.Now.Date, SalesChannel.POS);
        Assert.True(global.Current.Net >= 1070m);
        Assert.Equal(300m, global.Current.Cost);
        Assert.Contains(global.Stock, x => x.Warehouse == warehouse.Name);
        var empty = await service.BuildAsync("unknown-user", h.Clock.Now.Date, h.Clock.Now.Date, null);
        Assert.Empty(empty.Products); Assert.Empty(empty.Stock); Assert.Equal(0, empty.Visits); Assert.Equal(0, empty.Current.Orders);
    }
}
