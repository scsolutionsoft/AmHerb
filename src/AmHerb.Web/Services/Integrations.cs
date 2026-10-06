using System.Globalization;
using System.Net;
using System.Net.Mail;
using CsvHelper;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public record PaymentIntent(string Provider, string Reference, decimal Amount, string Instructions);
public interface IPaymentGateway { string Name { get; } Task<PaymentIntent> CreateAsync(string orderNumber, decimal amount); }
public class ManualBankTransferProvider(IConfiguration config) : IPaymentGateway
{
    public string Name => "ManualBankTransfer";
    public Task<PaymentIntent> CreateAsync(string number, decimal amount) => Task.FromResult(new PaymentIntent(Name, number, amount, config["Payments:BankInstructions"] ?? "ติดต่อ AM HERB เพื่อขอข้อมูลบัญชี และแจ้งเลขคำสั่งซื้อเมื่อชำระเงิน"));
}
public class PromptPayQrProvider : IPaymentGateway
{ public string Name => "PromptPay"; public Task<PaymentIntent> CreateAsync(string n, decimal a) => throw new BusinessException("ยังไม่ได้กำหนด PromptPay provider credentials"); }
public class CardGatewayProvider : IPaymentGateway
{ public string Name => "Card"; public Task<PaymentIntent> CreateAsync(string n, decimal a) => throw new BusinessException("ยังไม่ได้กำหนด Card provider credentials"); }
public interface IMarketplaceProvider { string Name { get; } bool Configured { get; } Task SyncAsync(CancellationToken ct); }
public class ShopeeProvider : IMarketplaceProvider { public string Name => "Shopee"; public bool Configured => false; public Task SyncAsync(CancellationToken ct) => Task.CompletedTask; }
public class TikTokShopProvider : IMarketplaceProvider { public string Name => "TikTokShop"; public bool Configured => false; public Task SyncAsync(CancellationToken ct) => Task.CompletedTask; }
public class LazadaProvider : IMarketplaceProvider { public string Name => "Lazada"; public bool Configured => false; public Task SyncAsync(CancellationToken ct) => Task.CompletedTask; }
public interface IShippingProvider { string Name { get; } string TrackingReference(string reference); }
public class ManualShippingProvider : IShippingProvider { public string Name => "Manual"; public string TrackingReference(string reference) => reference; }
public class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host)) throw new BusinessException("ยังไม่ได้ตั้งค่า SMTP สำหรับส่งอีเมล กรุณาติดต่อผู้ดูแล");
        using var client = new SmtpClient(host, config.GetValue("Smtp:Port", 587)) { EnableSsl = config.GetValue("Smtp:EnableSsl", true) };
        if (!string.IsNullOrWhiteSpace(config["Smtp:Username"])) client.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
        using var message = new MailMessage(config["Smtp:From"] ?? throw new BusinessException("SMTP From missing"), email, subject, htmlMessage) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }
}
public interface ILineNotifier { Task SendAsync(string userId, string message); }
public class LineNotifier : ILineNotifier { public Task SendAsync(string userId, string message) => throw new BusinessException("ยังไม่ได้ตั้งค่า LINE OA credentials"); }

public class MarketplaceRow
{
    public string Channel { get; set; } = "";
    public string ExternalOrderId { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string ReferralCode { get; set; } = "";
    public string Campaign { get; set; } = "";
    public string Creator { get; set; } = "";
    public decimal ChannelFee { get; set; }
    public decimal AffiliateFee { get; set; }
    public string FulfillmentStatus { get; set; } = "PendingPayment";
    public string TransactionRef { get; set; } = "";
    public decimal PaidAmount { get; set; }
    public string Carrier { get; set; } = "";
    public string TrackingNumber { get; set; } = "";
}
public class CsvMarketplaceImportProvider(IServiceScopeFactory scopes) : IMarketplaceProvider
{
    public string Name => "CSV"; public bool Configured => true; public Task SyncAsync(CancellationToken ct) => Task.CompletedTask;
    public async Task<IReadOnlyList<string>> ImportAsync(Stream stream, string actor)
    {
        using var reader = new StreamReader(stream); using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<MarketplaceRow>().Take(5001).ToList();
        if (rows.Count > 5000) throw new BusinessException("นำเข้าได้ไม่เกิน 5,000 แถวต่อครั้ง");
        var results = new List<string>();
        foreach (var group in rows.GroupBy(x => new { x.Channel, x.ExternalOrderId }))
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AmHerbDbContext>();
            var service = scope.ServiceProvider.GetRequiredService<CommerceService>();
            try
            {
                var r = group.First();
                if (!Enum.TryParse<SalesChannel>(r.Channel, out var channel) || channel is not (SalesChannel.Shopee or SalesChannel.TikTokShop or SalesChannel.Lazada) || string.IsNullOrWhiteSpace(r.ExternalOrderId) || r.ExternalOrderId.Length > 100) throw new BusinessException("ช่องทางหรือ external order id ไม่ถูกต้อง");
                if (r.FulfillmentStatus is not ("PendingPayment" or "Paid" or "Shipped" or "Delivered")) throw new BusinessException("FulfillmentStatus ไม่รองรับ");
                if (group.Any(x => x.ChannelFee != r.ChannelFee || x.AffiliateFee != r.AffiliateFee || x.PaidAmount != r.PaidAmount || x.CustomerName != r.CustomerName || x.FulfillmentStatus != r.FulfillmentStatus) || r.ChannelFee < 0 || r.AffiliateFee < 0) throw new BusinessException("ข้อมูลระดับคำสั่งซื้อต้องตรงกันทุกแถว");
                var lines = new List<SaleLine>();
                foreach (var row in group)
                {
                    var sku = await db.Skus.SingleOrDefaultAsync(x => x.Code == row.Sku) ?? throw new BusinessException("ไม่พบ SKU: " + row.Sku);
                    lines.Add(new SaleLine(sku.Id, row.Quantity));
                }
                var seller = await db.Members.SingleOrDefaultAsync(x => x.ReferralCode == r.ReferralCode && x.Status == MemberStatus.Active);
                var existing = await db.Orders.SingleOrDefaultAsync(x => x.Channel == channel && x.ExternalOrderId == r.ExternalOrderId);
                var order = existing ?? await service.CheckoutAsync(new CheckoutInput { Key = "csv:" + Guid.NewGuid().ToString("N"), CustomerName = r.CustomerName, Email = r.Email, Phone = r.Phone, Address = r.Address }, lines, null, seller?.Id, channel, externalId: r.ExternalOrderId, channelFee: r.ChannelFee, affiliateFee: r.AffiliateFee, campaign: r.Campaign, creator: r.Creator);
                if (r.FulfillmentStatus is "Paid" or "Shipped" or "Delivered")
                {
                    if (order.Status == OrderStatus.PendingPayment) await service.ConfirmPaymentAsync(order.Id, r.TransactionRef, r.PaidAmount, "Marketplace CSV evidence");
                    if ((r.FulfillmentStatus is "Shipped" or "Delivered") && (order.Status is OrderStatus.Paid or OrderStatus.Processing)) await service.FulfillAsync(order.Id, r.Carrier, r.TrackingNumber, false, "Marketplace CSV shipment");
                    if (r.FulfillmentStatus == "Delivered" && order.Status == OrderStatus.Shipped) await service.FulfillAsync(order.Id, r.Carrier, r.TrackingNumber, true, "Marketplace CSV delivery");
                }
                db.AuditLogs.Add(new AuditLog { ActorId = actor, Action = "Marketplace.Import", Subject = order.Id.ToString(), Detail = channel + ":" + r.ExternalOrderId });
                await db.SaveChangesAsync(); results.Add(r.ExternalOrderId + ": OK " + order.Number);
            }
            catch (BusinessException e) { results.Add(group.Key.ExternalOrderId + ": " + e.Message); }
            catch (Exception e) when (e is DbUpdateException || DatabaseConflict.IsDeadlock(e)) { results.Add(group.Key.ExternalOrderId + ": ข้อมูลซ้ำหรือมีการเปลี่ยนแปลงพร้อมกัน ตรวจสอบสถานะแล้วนำเข้าใหม่"); }
        }
        return results;
    }
}
