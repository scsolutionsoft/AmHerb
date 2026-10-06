namespace AmHerb.Web.Models;
public record FormField(string Name, string Label, string Type = "text", string Value = "", bool Required = true, Dictionary<string, string>? Options = null);
public record AdminForm(string Action, string Title, FormField[] Fields, string? Warning = null);
public static class AdminForms
{
    public static List<AdminForm> For(AdminPage m)
    {
        var sku = m.Skus.ToDictionary(x => x.Id.ToString(), x => x.Code);
        var warehouse = m.Warehouses.ToDictionary(x => x.Id.ToString(), x => x.Name);
        var channels = Enum.GetNames<Domain.SalesChannel>().ToDictionary(x => x);
        var optionalChannels = new Dictionary<string, string> { [""] = "ทุกช่องทาง" }; foreach (var c in channels) optionalChannels[c.Key] = c.Value;
        FormField F(string n, string l, string t = "text", string v = "", bool r = true) => new(n, l, t, v, r);
        FormField S(string n, string l, Dictionary<string, string> o, bool r = true) => new(n, l, "select", "", r, o);
        FormField Reason() => F("reason", "เหตุผล / หลักฐาน");
        FormField Utc(string n = "effectiveFrom", string l = "เริ่มใช้ UTC") => F(n, l, "datetime-local", DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture));
        var forms = new List<AdminForm>();
        switch (m.Section)
        {
            case "Members": case "Tree":
                forms.Add(new("Sponsor", "เปลี่ยนผู้แนะนำ", [F("memberId", "Member ID", "number"), F("sponsorId", "Sponsor ID (ว่าง = ไม่มี)", "number", r: false), Reason()], "การเปลี่ยนผู้แนะนำไม่เปลี่ยนผู้รับ Token ของยอดขายที่ผ่านมา"));
                forms.Add(new("SetMemberStatus", "สถานะและประเภทสมาชิก", [F("memberId", "Member ID", "number"), S("status", "สถานะ", Enum.GetNames<Domain.MemberStatus>().ToDictionary(x => x)), S("type", "ประเภท", Enum.GetNames<Domain.MemberType>().ToDictionary(x => x)), Reason()])); break;
            case "Prices": forms.Add(new("Price", "เพิ่มเวอร์ชันราคา", [S("skuId", "สินค้า", sku), S("tier", "ระดับราคา", new[] { "Retail", "Promo", "Wholesale", "Pack6", "Pack12" }.ToDictionary(x => x)), F("packQuantity", "จำนวนต่อแพ็ก", "number", "1"), F("amount", "ราคาแพ็ก", "number"), S("channel", "ช่องทาง", optionalChannels, false), Utc(), Reason()])); break;
            case "TokenRates": forms.Add(new("TokenRate", "เพิ่มเวอร์ชัน Base Token", [S("skuId", "สินค้า", sku), F("amount", "Base Token", "number"), S("channel", "ช่องทาง", optionalChannels, false), Utc(), Reason()], "อัตราใหม่ใช้กับยอดขายใหม่ตามวันที่เริ่มมีผล ไม่แก้ไขประวัติ")); break;
            case "TokenPolicies":
                var p = m.Policy!;
                string V(decimal v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
                forms.Add(new("Policy", "สร้างนโยบาย Token เวอร์ชันใหม่", [F("pendingDays", "รอปลดล็อก (0–60 วัน)", "number", p.PendingDays.ToString()), F("expiryMonths", "อายุ Token (เดือน)", "number", p.ExpiryMonths.ToString()), F("maxDepth", "ความลึก (สูงสุด 3)", "number", p.MaxDepth.ToString()), F("sellerPercent", "Seller %", "number", V(p.SellerPercent)), F("level1", "Level 1 %", "number", V(p.Level1Percent)), F("level2", "Level 2 %", "number", V(p.Level2Percent)), F("level3", "Level 3 %", "number", V(p.Level3Percent)), F("reference", "มูลค่าอ้างอิง THB/Token", "number", V(p.RedemptionReferenceThb)), F("maxRedemption", "แลกได้สูงสุด %", "number", V(p.MaxRedemptionPercent)), F("loyalty", "เปิด Loyalty ซื้อเอง", "checkbox", "true", false), F("loyaltyPercent", "Loyalty %", "number", V(p.LoyaltyPercent)), F("redemptionCategories", "หมวดหมู่ที่แลกได้ (คั่นด้วย , ว่าง = ทั้งหมด)", "text", p.RedemptionCategories, false), Utc(), Reason()], "ตรวจสอบผลกระทบก่อนเปลี่ยนนโยบาย: ห้ามรางวัลจากการสมัคร/สร้างสายงาน ห้ามโอนหรือถอน Token เป็นเงินสด และห้ามรางวัลเครือข่ายจากการซื้อเอง")); break;
            case "Ledger": forms.Add(new("ClearTokenReview", "ปิดการตรวจสอบยอด Token ที่ฟื้นแล้ว", [F("memberId", "Member ID", "number"), Reason()])); forms.Add(new("AdjustToken", "ปรับยอด Token พร้อมเหตุผล", [F("memberId", "Member ID", "number"), F("amount", "ยอดเพิ่ม/ลด", "number"), F("key", "รหัสรายการไม่ซ้ำ", "text", Guid.NewGuid().ToString("N")), Reason()], "ใช้แก้ไขข้อผิดพลาดหรือโปรโมชันเท่านั้น ไม่ใช่การโอนหรือถอนเงิน")); break;
            case "Inventory":
                forms.Add(new("Receive", "รับสินค้าเข้าล็อต", [S("skuId", "SKU", sku), S("warehouseId", "คลัง", warehouse), F("lot", "เลขล็อต"), F("mfg", "วันผลิต (UTC)", "date"), F("expiry", "วันหมดอายุ (UTC)", "date"), F("quantity", "จำนวน", "number"), F("cost", "ต้นทุน/ชิ้น", "number"), Reason()]));
                forms.Add(new("AdjustStock", "ปรับสต็อก", [F("batchId", "Batch ID", "number"), F("delta", "เพิ่ม/ลดจำนวน", "number"), S("kind", "ชนิด", new[] { "Adjust", "Damage", "Expire" }.ToDictionary(x => x)), Reason()])); break;
            case "Warehouses": forms.Add(new("Warehouse", "เพิ่มคลังสินค้า", [F("name", "ชื่อคลัง"), Reason()])); break;
            case "Payments": forms.Add(new("ConfirmPayment", "ยืนยันรับชำระเงิน", [F("orderId", "Order ID", "number"), F("reference", "เลขอ้างอิงธนาคาร/ผู้ให้บริการ"), F("amount", "จำนวนเงิน", "number"), Reason()])); break;
            case "Orders":
                forms.Add(new("Fulfill", "จัดส่ง / ยืนยันส่งมอบ", [F("orderId", "Order ID", "number"), F("carrier", "ขนส่ง", r: false), F("tracking", "เลขติดตาม", r: false), F("delivered", "ยืนยันส่งมอบแล้ว", "checkbox", "true", false), Reason()])); break;
            case "Returns": forms.Add(new("ApproveReturn", "อนุมัติคืนสินค้าและบันทึกคืนเงิน", [F("requestId", "Return ID", "number"), F("sellable", "สินค้าสภาพขายต่อได้", "checkbox", "true", false), F("reference", "หลักฐานอ้างอิงคืนเงิน"), Reason()], "ฝ่ายการเงินต้องคืนเงินผ่านวิธีชำระเดิมก่อนบันทึก ระบบย้อน Token และคืนสต็อกในธุรกรรมเดียวกัน")); break;
            case "Promotions":
                var optionalSkus = new Dictionary<string, string> { [""] = "ไม่ระบุ" }; foreach (var s in sku) optionalSkus[s.Key] = s.Value;
                var types = new Dictionary<string, string> { [""] = "ทุกประเภท" }; foreach (var t in Enum.GetNames<Domain.MemberType>()) types[t] = t;
                forms.Add(new("Promotion", "สร้างโปรโมชัน", [F("code", "รหัสคูปอง"), S("kind", "ชนิด", Enum.GetNames<Domain.PromotionKind>().ToDictionary(x => x)), F("value", "ส่วนลดบาท / % / บาทต่อชุด", "number", "0"), F("minimumSpend", "ยอดขั้นต่ำ", "number", "0"), F("requiredQuantity", "จำนวนต่อชุด", "number", "1"), S("eligibleSkuId", "สินค้าร่วมรายการ", optionalSkus, false), S("giftSkuId", "ของแถม", optionalSkus, false), S("channel", "ช่องทาง", optionalChannels, false), S("memberType", "ประเภทสมาชิก", types, false), F("usageLimit", "จำนวนใช้สูงสุด", "number", "100"), F("tokenMultiplier", "ตัวคูณ Base Token", "number", "1"), Utc(), Utc("effectiveTo", "สิ้นสุด UTC"), Reason()], "หนึ่งคูปองต่อคำสั่งซื้อ ห้ามซ้อนโปรโมชัน เปลี่ยนเงื่อนไขโดยสร้างรหัสใหม่เพื่อคงประวัติ"));
                forms.Add(new("DisablePromotion", "หยุดโปรโมชัน", [F("id", "Promotion ID", "number"), Reason()])); break;
            case "POS": forms.Add(new("Register", "กำหนดคลังและสิทธิ์ POS รายผู้ใช้", [F("registerId", "Register ID", "number"), S("warehouseId", "คลัง", warehouse), F("enabled", "เปิดใช้งาน", "checkbox", "true", false), Reason()])); break;
            case "Settings":
                forms.Add(new("Tax", "อัตราภาษีเพิ่มจากราคาสินค้า", [F("percent", "ภาษี % (0 = ไม่คิดเพิ่ม)", "number", "0"), Reason()]));
                forms.Add(new("Setting", "การตั้งค่าระบบ", [S("key", "ค่า", new[] { "ReferralCookieDays", "AbandonedCheckoutHours" }.ToDictionary(x => x)), F("value", "จำนวนวัน / ชั่วโมง", "number"), Reason()]));
                forms.Add(new("Channel", "นโยบายช่องทาง", [S("channel", "ช่องทาง", channels), F("network", "อนุญาตรางวัลเครือข่าย", "checkbox", "true", false), F("redemption", "อนุญาตแลก Token", "checkbox", "true", false), Reason()], "Marketplace affiliate ควรปิดรางวัลเครือข่ายเพื่อป้องกันค่าคอมมิชชันซ้ำ"));
                forms.Add(new("Role", "กำหนดสิทธิ์ผู้ใช้ (SuperAdmin)", [F("email", "อีเมล", "email"), S("role", "บทบาท", Data.SeedData.Roles.ToDictionary(x => x)), F("grant", "เพิ่มสิทธิ์ (ไม่เลือก = ถอน)", "checkbox", "true", false), Reason()])); break;
            case "Products": forms.Add(new("Shipping", "ค่าจัดส่ง", [F("fee", "ค่าจัดส่ง", "number", "50"), F("freeAbove", "ส่งฟรีเมื่อยอดสุทธิถึง", "number", "1500"), Reason()])); break;
        }
        return forms;
    }
}

