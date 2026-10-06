using System.Data;
using System.ComponentModel.DataAnnotations;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

public record SaleLine(long SkuId, int Quantity, string Tier = "Retail");
public class CheckoutInput
{
    [Required, MaxLength(100)] public string Key { get; set; } = Guid.NewGuid().ToString("N");
    [Required, MaxLength(160)] public string CustomerName { get; set; } = "";
    [EmailAddress, MaxLength(200)] public string? Email { get; set; }
    [Required, MaxLength(40)] public string Phone { get; set; } = "";
    [MaxLength(1000)] public string Address { get; set; } = "";
    [MaxLength(60)] public string? Coupon { get; set; }
    [Range(0, 1000000)] public decimal Tokens { get; set; }
    public bool StockLoading { get; set; }
    public bool UseCredit { get; set; }
    [MaxLength(64)] public string? RedemptionCode { get; set; }
}
public class CommerceService(AmHerbDbContext db, PricingService pricing, RewardService rewards, InventoryService inventory,
    AuditService audit, TimeProvider clock, INotificationService notifications)
{
    public async Task<Order> CheckoutAsync(CheckoutInput input, IReadOnlyCollection<SaleLine> lines, long? buyerId, long? sellerId,
        SalesChannel channel = SalesChannel.Store, string? cashierId = null, long? sessionId = null, decimal tendered = 0,
        string paymentMethod = "Cash", string? externalId = null, decimal channelFee = 0, decimal affiliateFee = 0, string campaign = "", string creator = "")
    {
        if (string.IsNullOrWhiteSpace(input.Email)) input.Email = null;
        if (!Validator.TryValidateObject(input, new ValidationContext(input), [], true)) throw new BusinessException("ข้อมูลลูกค้าหรือคำสั่งซื้อไม่ถูกต้อง");
        if (input.Tokens < 0 || Money.Round(input.Tokens) != input.Tokens || string.IsNullOrWhiteSpace(input.Key)) throw new BusinessException("ข้อมูลตะกร้าไม่ถูกต้อง");
        if(input.UseCredit&&(channel!=SalesChannel.Store||buyerId==null))throw new BusinessException("เครดิตบริษัทใช้ได้เฉพาะสมาชิกที่ซื้อผ่านหน้าร้านบริษัท");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var prior = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.IdempotencyKey == input.Key);
        if (prior != null)
        {
            if ((prior.BuyerMemberId != buyerId && !(channel == SalesChannel.POS && buyerId == null)) || prior.CashierUserId != cashierId || prior.Channel != channel) throw new BusinessException("รหัสรายการถูกใช้แล้ว");
            return prior;
        }
        if (lines.Count == 0 || lines.Count > 100 || lines.Any(x => x.Quantity < 1 || x.Quantity > 10000)) throw new BusinessException("ข้อมูลตะกร้าไม่ถูกต้อง");
        long warehouseId = 1;
        RedemptionAuthorization? redemptionApproval = null;
        if (channel == SalesChannel.POS)
        {
            var session = await db.PosSessions.Include(x => x.PosRegister).SingleOrDefaultAsync(x => x.Id == sessionId && x.PosRegister.UserId == cashierId && x.PosRegister.Enabled && x.ClosedAt == null) ?? throw new BusinessException("กรุณาเปิดกะ POS ของคุณก่อนขาย");
            warehouseId = session.PosRegister.WarehouseId;
            if (input.Tokens > 0)
            {
                var hash = RedemptionConsentService.Hash(input.RedemptionCode ?? "");
                redemptionApproval = await db.RedemptionAuthorizations.SingleOrDefaultAsync(x => x.CodeHash == hash && x.PosRegisterId == session.PosRegisterId && x.UsedOrderId == null && x.ExpiresAt > clock.GetUtcNow().UtcDateTime);
                if (redemptionApproval == null || input.Tokens > redemptionApproval.MaximumTokens || (buyerId != null && buyerId != redemptionApproval.MemberId)) throw new BusinessException("รหัสอนุมัติแลก Token ไม่ถูกต้อง หมดอายุ หรือไม่ตรงกับจุดขาย/ผู้ซื้อ");
                buyerId = redemptionApproval.MemberId;
            }
            if (paymentMethod is not ("Cash" or "ManualBankTransfer")) throw new BusinessException("วิธีชำระเงินไม่รองรับ");
        }
        if (buyerId != null && !await db.Members.AnyAsync(x => x.Id == buyerId && x.Status == MemberStatus.Active)) throw new BusinessException("สมาชิกผู้ซื้อไม่พร้อมใช้งาน");
        if(input.UseCredit)await new CreditService(db,audit,clock,notifications).LockForPurchase(buyerId!.Value);
        if (sellerId != null && !await db.Members.AnyAsync(x => x.Id == sellerId && x.Status == MemberStatus.Active)) sellerId = null;
        var now = clock.GetUtcNow().UtcDateTime;
        var policy = await pricing.PolicyAsync();
        var order = new Order { Number = "AM" + now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(), IdempotencyKey = input.Key,
            BuyerMemberId = buyerId, SellerMemberId = sellerId, Channel = channel, CashierUserId = cashierId, PosSessionId = sessionId,
            CustomerName = input.CustomerName, Email = input.Email?.Trim() ?? "", Phone = input.Phone.Trim(), Address = input.Address,
            StockLoading = input.StockLoading, TokenRedemption = input.Tokens, TokenPolicyVersion = policy.Id, CreatedAt = now,
            ExternalOrderId = externalId, ChannelFee = channelFee, AffiliateFee = affiliateFee, Campaign = campaign, Creator = creator };
        foreach (var line in lines.GroupBy(x => new { x.SkuId, x.Tier }).Select(g => new SaleLine(g.Key.SkuId, g.Sum(x => x.Quantity), g.Key.Tier)))
        {
            if (line.Quantity > 10000) throw new BusinessException("จำนวนสินค้ามากเกินไป");
            var sku = await db.Skus.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == line.SkuId && x.Active && x.Product.Active) ?? throw new BusinessException("สินค้าไม่พร้อมจำหน่าย");
            if (line.Tier != "Retail" && buyerId == null) throw new BusinessException("ราคาสมาชิกต้องระบุบัญชีสมาชิกผู้ซื้อ");
            var price = await pricing.PriceAsync(sku.Id, channel, line.Tier);
            if (line.Tier is "Wholesale" or "Pack6" or "Pack12") order.StockLoading = true;
            if (price.PackQuantity < 1 || Money.Round(price.Amount / price.PackQuantity) * price.PackQuantity != price.Amount) throw new BusinessException("ราคาแพ็กต้องหารเป็นราคาต่อชิ้นได้ถึงสองตำแหน่ง กรุณาติดต่อผู้ดูแล");
            var rate = await pricing.RateAsync(sku.Id, channel);
            order.Items.Add(new OrderItem { Sku = sku, SkuId = sku.Id, Name = sku.Product.Name + " / " + sku.Variant + " / " + line.Tier, Quantity = checked(line.Quantity * price.PackQuantity), UnitPrice = price.Amount / price.PackQuantity, BaseToken = rate.BaseToken, TokenRateVersion = rate.Id, PriceVersion = price.Id });
        }
        order.Merchandise = order.Items.Sum(x => x.UnitPrice * x.Quantity);
        if (!string.IsNullOrWhiteSpace(input.Coupon)) await ApplyPromotionAsync(order, input.Coupon.Trim().ToUpperInvariant(), now);
        if (order.PromotionId == null) Allocate(order.Discount, order.Items.Where(x => !x.IsGift).ToArray(), x => x.UnitPrice * x.Quantity, (x, amount) => x.Discount = amount);
        var net = order.Merchandise - order.Discount;
        if (channel == SalesChannel.Store)
        {
            if (string.IsNullOrWhiteSpace(input.Address)) throw new BusinessException("กรุณากรอกที่อยู่จัดส่ง");
            var shipping = await db.ShippingRules.Where(x => x.Active).OrderBy(x => x.Id).FirstAsync();
            order.Shipping = net >= shipping.FreeAbove ? 0 : shipping.Fee;
        }
        var categories = policy.RedemptionCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var eligible = order.Items.Where(x => x.Sku.TokenEligible && !x.IsGift && (categories.Length == 0 || categories.Contains(x.Sku.Product.Category, StringComparer.OrdinalIgnoreCase))).ToArray();
        var eligibleTotal = eligible.Sum(x => x.UnitPrice * x.Quantity - x.Discount);
        order.TokenValue = Money.Round(order.TokenRedemption * policy.RedemptionReferenceThb);
        if (order.TokenValue > eligibleTotal) throw new BusinessException("Token เกินมูลค่าสินค้าที่ใช้แลกได้");
        Allocate(order.TokenValue, eligible, x => x.UnitPrice * x.Quantity - x.Discount, (x, amount) => x.TokenValue = amount);
        var taxRate = decimal.Parse((await db.SystemSettings.SingleAsync(x => x.Key == "TaxRatePercent")).Value, System.Globalization.CultureInfo.InvariantCulture);
        order.Tax = Money.Round(net * taxRate / 100);
        Allocate(order.Tax, order.Items.Where(x => !x.IsGift).ToArray(), x => x.UnitPrice * x.Quantity - x.Discount, (x, value) => x.Tax = value);
        order.GrandTotal = net + order.Shipping + order.Tax;
        order.CashPayable = order.GrandTotal - order.TokenValue;
        db.Orders.Add(order); await db.SaveChangesAsync();
        if (redemptionApproval != null) redemptionApproval.UsedOrderId = order.Id;
        foreach (var item in order.Items) await inventory.ReserveAsync(item, warehouseId);
        await rewards.ReserveAsync(order, eligibleTotal);
        var payment = new Payment { OrderId = order.Id, Amount = order.CashPayable, Provider = input.UseCredit&&order.CashPayable>0 ? "TradeCredit" : channel == SalesChannel.POS ? paymentMethod : "ManualBankTransfer" };
        db.Payments.Add(payment); await db.SaveChangesAsync();
        // Cash POS can settle immediately; transfers require Finance confirmation.
        if(payment.Provider=="TradeCredit")
        {
            await new CreditService(db,audit,clock,notifications).Issue(order);
            payment.Status="CreditApproved";payment.TransactionRef="CREDIT-"+order.Number;
            order.Status=OrderStatus.Processing;
            await inventory.FinalizeAsync(order,true);await rewards.ConsumeAsync(order);
            await new AccountingService(db).Sale(order,payment);
        }
        else if (channel == SalesChannel.POS && paymentMethod == "Cash")
        {
            if (tendered < order.CashPayable || tendered > 100000000 || Money.Round(tendered) != tendered) throw new BusinessException("จำนวนเงินรับไม่ถูกต้อง");
            payment.Tendered = tendered; payment.Change = tendered - order.CashPayable;
            await SettleAsync(order, payment, "POS-" + order.Number);
            order.Status = OrderStatus.Delivered;
        }
        else if (order.CashPayable == 0) await SettleAsync(order, payment, "TOKEN-" + order.Number);
        if (cashierId != null) audit.Add("POS.Sale", order.Id, order.Number);
        await notifications.AddAsync(buyerId, "order:" + order.Id, $"สร้างคำสั่งซื้อ {order.Number}");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return order;
    }
    private static void Allocate(decimal total, OrderItem[] items, Func<OrderItem, decimal> weight, Action<OrderItem, decimal> assign)
    {
        var basis = items.Sum(weight); decimal remaining = total;
        for (var i = 0; i < items.Length; i++)
        { var value = i == items.Length - 1 ? remaining : basis <= 0 ? 0 : Math.Min(remaining, Money.Round(total * weight(items[i]) / basis)); assign(items[i], value); remaining -= value; }
    }
    private async Task ApplyPromotionAsync(Order order, string code, DateTime now)
    {
        var promo = await db.Promotions.SingleOrDefaultAsync(x => x.Code == code && x.Active && x.EffectiveFrom <= now && x.EffectiveTo > now && x.UsedCount < x.UsageLimit) ?? throw new BusinessException("คูปองไม่พร้อมใช้งาน");
        if (promo.Channel != null && promo.Channel != order.Channel) throw new BusinessException("คูปองไม่รองรับช่องทางนี้");
        if (promo.MemberType != null && !await db.Members.AnyAsync(x => x.Id == order.BuyerMemberId && x.Type == promo.MemberType)) throw new BusinessException("ประเภทสมาชิกไม่ร่วมรายการ");
        var qualifying = order.Items.Where(x => promo.EligibleSkuId == null || x.SkuId == promo.EligibleSkuId).ToArray();
        var basis = qualifying.Sum(x => x.UnitPrice * x.Quantity);
        if (order.Merchandise < promo.MinimumSpend || qualifying.Sum(x => x.Quantity) < promo.RequiredQuantity) throw new BusinessException("ยอดหรือจำนวนสินค้ายังไม่ถึงเงื่อนไขคูปอง");
        order.Discount = promo.Kind switch { PromotionKind.Fixed => Math.Min(basis, promo.Value), PromotionKind.Percentage => Money.Round(basis * promo.Value / 100), PromotionKind.Bundle => Math.Min(basis, promo.Value * (qualifying.Sum(x => x.Quantity) / promo.RequiredQuantity)), _ => 0 };
        Allocate(order.Discount, qualifying, x => x.UnitPrice * x.Quantity, (x, amount) => x.Discount = amount);
        if (promo.Kind == PromotionKind.FreeGift)
        {
            var gift = await db.Skus.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == promo.GiftSkuId && x.Active && x.Product.Active) ?? throw new BusinessException("ของแถมไม่พร้อมจำหน่าย");
            order.Items.Add(new OrderItem { Sku = gift, SkuId = gift.Id, Name = gift.Product.Name + " (ของแถม)", Quantity = 1, IsGift = true });
        }
        foreach (var item in qualifying) item.TokenMultiplier = promo.TokenMultiplier;
        promo.UsedCount++; order.PromotionId = promo.Id;
    }
    private async Task SettleAsync(Order order, Payment payment, string reference)
    {
        if (payment.Status == "Confirmed") return;
        if (order.Status != OrderStatus.PendingPayment) throw new BusinessException("คำสั่งซื้อไม่อยู่ในสถานะรอชำระ");
        payment.Status = "Confirmed"; payment.TransactionRef = reference; payment.ConfirmedAt = clock.GetUtcNow().UtcDateTime;
        order.Status = OrderStatus.Paid;
        await inventory.FinalizeAsync(order, true); await rewards.ConsumeAsync(order);
        await new AccountingService(db).Sale(order, payment);
    }
    public async Task ConfirmPaymentAsync(long orderId, string reference, decimal amount, string reason)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 200) throw new BusinessException("กรุณาระบุเลขอ้างอิงการชำระเงิน");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId);
        var payment = await db.Payments.SingleAsync(x => x.OrderId == orderId);
        if(payment.Provider=="TradeCredit")throw new BusinessException("รายการซื้อเชื่อต้องรับชำระผ่านระบบเครดิตสมาชิก");
        if(await db.CreditReceipts.AnyAsync(x=>x.ConfirmedReference==reference.Trim().ToUpperInvariant()))throw new BusinessException("เลขอ้างอิงโอนนี้ใช้รับชำระเครดิตแล้ว");
        if (payment.Status == "Confirmed") { if (payment.TransactionRef != reference || payment.Amount != amount) throw new BusinessException("รายการนี้ยืนยันด้วยข้อมูลอื่นแล้ว"); return; }
        if (amount != order.CashPayable) throw new BusinessException("ยอดเงินไม่ตรงกับคำสั่งซื้อ");
        await SettleAsync(order, payment, reference);
        if (order.Channel == SalesChannel.POS) order.Status = OrderStatus.Delivered;
        audit.Add("Payment.Confirm", orderId, reason);
        await notifications.AddAsync(order.BuyerMemberId, "paid:" + orderId, $"ยืนยันชำระเงิน {order.Number}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task FulfillAsync(long orderId, string carrier, string tracking, bool delivered, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await db.Orders.SingleAsync(x => x.Id == orderId);
        if (delivered)
        {
            if (order.Status != OrderStatus.Shipped) throw new BusinessException("ต้องจัดส่งสินค้าก่อนยืนยันส่งมอบ");
            order.Status = OrderStatus.Delivered;
            foreach (var s in await db.Shipments.Where(x => x.OrderId == orderId).ToListAsync()) s.Status = "Delivered";
        }
        else
        {
            if (order.Status is not (OrderStatus.Paid or OrderStatus.Processing) || string.IsNullOrWhiteSpace(carrier) || string.IsNullOrWhiteSpace(tracking)) throw new BusinessException("กรุณาตรวจสอบสถานะและข้อมูลจัดส่ง");
            order.Status = OrderStatus.Shipped; db.Shipments.Add(new Shipment { OrderId = orderId, Carrier = carrier, TrackingNumber = tracking });
        }
        audit.Add("Order.Fulfill", orderId, reason);
        await notifications.AddAsync(order.BuyerMemberId, $"shipping:{orderId}:{order.Status}", $"{order.Number}: {order.Status}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task VerifyRetailAsync(long orderId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("ต้องระบุหลักฐานยืนยันการขายปลีก");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId);
        if (order.RewardsCreated) return;
        if(await db.CreditInvoices.AnyAsync(x=>x.OrderId==orderId&&x.Amount>x.Paid+x.Adjusted))throw new BusinessException("ต้องชำระหนี้ของคำสั่งซื้อนี้ครบก่อนยืนยันรางวัล");
        if (order.Status != OrderStatus.Delivered || order.StockLoading) throw new BusinessException("ยืนยันได้เฉพาะยอดขายปลีกที่ส่งมอบแล้ว ไม่ใช่การซื้อสะสมสต็อก");
        order.VerifiedRetailSale = true; order.Status = OrderStatus.Completed; order.CompletedAt = clock.GetUtcNow().UtcDateTime;
        await rewards.CreateForOrderAsync(order);
        audit.Add("Order.VerifyRetail", orderId, reason); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task CancelAsync(long orderId, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId);
        if (order.Status == OrderStatus.Cancelled) return;
        if (order.Status != OrderStatus.PendingPayment) throw new BusinessException("รายการชำระแล้วต้องใช้ขั้นตอนคืนสินค้า/คืนเงิน");
        await inventory.FinalizeAsync(order, false); await rewards.CancelReservationAsync(order);
        order.Status = OrderStatus.Cancelled;
        foreach (var payment in await db.Payments.Where(x => x.OrderId == orderId).ToListAsync()) payment.Status = "Cancelled";
        if (order.PromotionId != null) (await db.Promotions.SingleAsync(x => x.Id == order.PromotionId)).UsedCount--;
        audit.Add("Order.Cancel", orderId, reason); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task RequestReturnAsync(long itemId, int quantity, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await db.OrderItems.Include(x => x.Order).SingleAsync(x => x.Id == itemId);
        if (item.Order.Status is not (OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.ReturnRequested or OrderStatus.Returned)) throw new BusinessException("รายการยังไม่อยู่ในสถานะที่คืนได้");
        var pending = await db.ReturnRequests.Where(x => x.OrderItemId == itemId && x.Status == "Requested").SumAsync(x => x.Quantity);
        if (quantity <= 0 || quantity + pending + item.ReturnedQuantity > item.Quantity || string.IsNullOrWhiteSpace(reason)) throw new BusinessException("จำนวนคืนหรือเหตุผลไม่ถูกต้อง");
        db.ReturnRequests.Add(new ReturnRequest { OrderItemId = itemId, Quantity = quantity, Reason = reason });
        item.Order.Status = OrderStatus.ReturnRequested;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task ApproveReturnAsync(long requestId, bool sellable, string refundRef, string reason)
    {
        if (string.IsNullOrWhiteSpace(refundRef)) throw new BusinessException("ต้องระบุหลักฐานการคืนเงิน");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await db.ReturnRequests.Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Items).SingleAsync(x => x.Id == requestId);
        if (request.Status == "Approved") return;
        var item = request.OrderItem; var order = item.Order;
        if (item.ReturnedQuantity + request.Quantity > item.Quantity) throw new BusinessException("จำนวนคืนเกินสินค้าที่ขาย");
        item.ReturnedQuantity += request.Quantity;
        var prior = await db.ReturnRequests.Where(x => x.OrderItemId == item.Id && x.Status == "Approved").ToListAsync();
        var merchandiseRefund = Money.Round((item.UnitPrice * item.Quantity - item.Discount - item.TokenValue + item.Tax) * item.ReturnedQuantity / item.Quantity);
        request.CashRefund = merchandiseRefund - prior.Sum(x => x.CashRefund);
        var policy = await db.TokenPolicies.SingleAsync(x => x.Id == order.TokenPolicyVersion);
        request.TokensRestored = Money.Round(item.TokenValue / policy.RedemptionReferenceThb * item.ReturnedQuantity / item.Quantity) - prior.Sum(x => x.TokensRestored);
        var full = order.Items.All(x => x.ReturnedQuantity == x.Quantity);
        if (full)
        {
            var other = await db.ReturnRequests.Where(x => x.OrderItem.OrderId == order.Id && x.Status == "Approved").ToListAsync();
            request.CashRefund = order.CashPayable - other.Sum(x => x.CashRefund);
            request.TokensRestored = order.TokenRedemption - other.Sum(x => x.TokensRestored);
        }
        request.Sellable = sellable; request.Status = "Approved"; request.RefundReference = refundRef; request.ApprovedAt = clock.GetUtcNow().UtcDateTime;
        var restoredCost = await inventory.ReturnAsync(item, request.Quantity, sellable, reason);
        await rewards.ReverseItemAsync(item, request.Id);
        await rewards.RestoreRedemptionAsync(order, request.TokensRestored, request.Id);
        var creditReduction=await new CreditService(db,audit,clock,notifications).ApplyReturn(order,request.CashRefund);
        await new AccountingService(db).Refund(request, policy.RedemptionReferenceThb, restoredCost,creditReduction);
        order.Status = full ? OrderStatus.Refunded : order.RewardsCreated ? OrderStatus.Completed : OrderStatus.Delivered;
        audit.Add("Return.Approve", requestId, reason + "; refund reference: " + refundRef);
        await notifications.AddAsync(order.BuyerMemberId, "return:" + requestId, $"คืนสินค้า {order.Number}: ลดหนี้เครดิต {creditReduction:N2}, คืนเงิน {request.CashRefund-creditReduction:N2}, Token {request.TokensRestored:N2}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}

