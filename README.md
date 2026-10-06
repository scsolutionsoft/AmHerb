# AM HERB Member Commerce, Reward Network & User POS

ASP.NET Core MVC / .NET 8, SQL Server + EF Core 8, ASP.NET Core Identity, Razor/Tag Helpers และ Bootstrap 5.3.8 โทนเขียว–ทอง หน้าจอภาษาไทย รองรับ desktop/mobile และเตรียม localization ผ่าน ASP.NET Core localization

## โมดูล

- ร้านค้า: สินค้า/ตัวเลือก/ราคา effective-date, ราคาสมาชิก/แพ็ก, ตะกร้า guest/member, คูปอง, ที่อยู่, ค่าส่ง, ภาษี, ติดตามสถานะ
- Identity: สมัครสมาชิก ยืนยันอีเมล เข้าสู่ระบบ lockout รีเซ็ตรหัสผ่าน MFA และ Google OAuth เมื่อกำหนดค่า
- สมาชิก: Sponsor/closure tree, ป้องกันวงจร, referral cookie แบบ last-click, QR, ยอดส่วนตัว/ทีม, wallet/statement และแจ้งเตือน
- POS ต่อ USER: จุดขายส่วนตัว, คลังที่ได้รับมอบหมาย, เปิด/ปิดกะ, ค้นหา/สแกนบาร์โค้ด, เงินสด/เงินทอน, โอนเงินรอ Finance, ใบเสร็จ/ใบจัดสินค้า, รายการขายและกะของตนเอง
- แลก Token ที่ POS: ลูกค้าสร้างรหัสอนุมัติจากบัญชีตนเอง ผูกจุดขายและยอดสูงสุด ใช้ได้ครั้งเดียวใน 5 นาที เก็บเฉพาะ hash รหัสในฐานข้อมูล
- สต็อก: คลัง/ล็อต/วันผลิต/หมดอายุ, FEFO, จอง/ขาย/ปล่อยจอง/รับคืน, stock card, ปรับเสียหาย/หมดอายุ, แจ้งเตือน 30/60/90 วัน
- Reward: Seller 100%, L1 20%, L2 7%, L3 3%, pending 14 วันเริ่มต้น, snapshot ผู้รับและเวอร์ชัน, release/expiry แบบ idempotent, append-only ledger
- คืนสินค้า: request/approval, คืนบางส่วน, ย้อนรางวัลผู้รับเดิม, คืน Token ที่ใช้ซื้อ, ยอดติดลบและระงับแลกจนฟื้น
- หลังบ้าน: สมาชิก, สินค้า, ราคา, นโยบาย/อัตรา Token, คำสั่งซื้อ, การเงิน, สต็อก, POS, โปรโมชั่น, CSV marketplace, รายงาน/CSV, audit และ settings
- REST API `/api/v1`: auth/profile, products/prices, cart/orders, hierarchy, token/quote, referral, POS และ consent
- BackgroundService: ปลดล็อก/หมดอายุ Token, ยกเลิก checkout หมดเวลา, ล้างตะกร้าเก่า, แจ้งเตือนคลัง, daily snapshots, ช่องเชื่อม marketplace sync

## เริ่มใช้งาน

ตั้งค่า `ConnectionStrings:DefaultConnection` สำหรับ SQL Server ผ่าน .NET User Secrets หรือ environment variable ก่อนเริ่มใช้งาน ไม่เก็บรหัสผ่านใน repository

Repository นี้มีซอร์สโค้ด migrations และคู่มือ ไม่รวมข้อมูลสมาชิก ยอดขาย สลิป รูปภาพสินค้าที่เก็บในฐานข้อมูล หรือไฟล์ผลลัพธ์ใน `artifacts/` เครื่องใหม่ต้องตั้งค่าฐานข้อมูลและนำเข้ารูปภาพของตนเอง

```powershell
dotnet tool restore
dotnet restore AmHerb.sln
dotnet build AmHerb.sln
dotnet run --project src/AmHerb.Web --launch-profile http -- --check-database
dotnet run --project src/AmHerb.Web --launch-profile http -- --migrate
dotnet run --project src/AmHerb.Web --launch-profile https
```

เปิด https://localhost:7033 หรือใช้ profile `http` ที่ http://localhost:5087 สำหรับพัฒนา

เครื่องใหม่ตั้ง `ConnectionStrings__DefaultConnection` เป็น environment variable หรือใช้ .NET User Secrets ของ `src/AmHerb.Web` ใน Development ดูรูปแบบตัวเลือกที่ `src/AmHerb.Web/appsettings.Example.json` ห้ามใส่รหัสผ่านจริงลงไฟล์ตัวอย่าง

การเริ่มเว็บปกติ **ไม่รัน migrations อัตโนมัติ** ต้องรัน `--migrate` ครั้งแรกก่อน เว็บจะตรวจ/สร้าง roles และ bootstrap admin ตาม environment เท่านั้น

### SuperAdmin แรก

ไม่ seed รหัสผ่านที่ทราบล่วงหน้า ป้อนอีเมลและรหัสผ่านแบบซ่อนจาก terminal:

```powershell
dotnet run --project src/AmHerb.Web --no-build --launch-profile http -- --setup-admin
```

หรือใช้สคริปต์เมื่อเครื่องอนุญาตให้รัน PowerShell scripts:

```powershell
.\scripts\Set-Admin.ps1 -Email 'your-admin@example.com'
```

สคริปต์ตั้ง `AMHERB_ADMIN_EMAIL` และ `AMHERB_ADMIN_PASSWORD` เฉพาะ process แล้วคืนค่าเดิมเมื่อเสร็จ Bootstrap สร้างเฉพาะอีเมลใหม่ ไม่ยกระดับบัญชี public ที่มีอยู่แล้ว บัญชี admin ได้รับ email confirmation และ POS register สำหรับเริ่มระบบ

### การเตรียมขาย

1. เข้าหลังบ้าน รับสต็อกจริงพร้อมล็อต วันผลิต วันหมดอายุ และต้นทุน (ไม่ seed สต็อกสมมติ)
2. เติมข้อมูลฉลาก เลขทะเบียน ผู้ผลิต รูปภาพ และราคาที่ได้รับอนุมัติ สินค้า Detoxify Blue เว้นราคา wholesale/แพ็กไว้ตามข้อกำหนด
3. ตั้งคำแนะนำบัญชีรับโอน `Payments:BankInstructions` และ SMTP สำหรับยืนยันอีเมล/รีเซ็ตรหัสผ่าน
4. แต่ละผู้ใช้ที่สมัครสำเร็จมี POS register ของตนเอง ผูกคลังส่วนตัวเริ่มต้น Admin เปลี่ยนคลัง/ปิดสิทธิ์ได้จากหน้า POS หลังบ้าน ต้องปิดกะก่อนเปลี่ยนคลัง
5. ผู้ใช้เปิดกะ เลือกสินค้า รับเงิน และพิมพ์ใบเสร็จ สต็อกเป็นของคลังที่ผูกกับจุดขาย ไม่แยกเป็นสต็อกส่วนตัวโดยอัตโนมัติ
6. ยอดโอนต้องให้ Finance/Admin ยืนยัน แคชเชียร์ยืนยันยอดขายปลีกเพื่อให้ Token เองไม่ได้
7. ยืนยันส่งมอบแล้ว Finance/Admin ตรวจหลักฐานและกด Verify retail เพื่อสร้างรางวัล Pending

## กฎสำคัญ

- ไม่ให้รางวัลจากการสมัคร การสร้าง sponsor ค่าสมาชิก หรือการซื้อสะสมสต็อก
- ซื้อเองไม่ให้รางวัลเครือข่าย โดยตรวจ buyer identity และข้อมูลติดต่อที่ตรงกับผู้ขาย Loyalty ปิดเริ่มต้น
- Wholesale/Pack checkout ทำเครื่องหมาย stock-loading เสมอ ไม่สร้างค่าคอมมิชชันอัตโนมัติ
- Marketplace ปิด network/redeem เริ่มต้นเพื่อป้องกันค่าคอมมิชชันซ้ำ เปลี่ยนได้ด้วยสิทธิ์และ audit
- Token แลกเฉพาะสินค้าที่กำหนด/หมวดหมู่/ช่องทางตามนโยบาย ไม่ใช้กับ shipping/tax ไม่มี endpoint ถอนเงินหรือโอนระหว่างสมาชิก
- ภาษีเริ่มต้น 0% ถ้าตั้งมากกว่า 0 ระบบคิดเพิ่มจากราคาสุทธิหลังส่วนลด ใบเสร็จเป็นใบรับเงิน ไม่ใช่ใบกำกับภาษี
- Refund ของ POS ทำโดย Finance นอกลิ้นชักแคชเชียร์ ยอดปิดกะ = เงินเปิด + เงินสดรับจากยอดขาย ไม่รวมเงินโอนหรือการคืนเงินจากฝ่ายการเงิน
- จองสินค้าใน checkout ขายสต็อกเมื่อรับชำระ ปล่อยจองเมื่อยกเลิกคำสั่งซื้อที่ยังไม่ชำระ
- Ledger, audit และ inventory history ป้องกัน UPDATE/DELETE ทั้ง EF และ SQL triggers
- เวลาเก็บ UTC (`datetime2`) แสดง Asia/Bangkok; ฟอร์มวันที่ตั้งนโยบายระบุ UTC ชัดเจน

## ทดสอบ

```powershell
.\scripts\Test.ps1                  # unit tests; SQL/browser tests skipped
.\scripts\Test.ps1 -Sql             # actual SQL Server integration tests
.\scripts\Test.ps1 -Browser         # SQL + headless Chrome desktop/mobile
```

SQL tests ใช้ server credentials จาก secrets/environment แต่ **สร้าง database ชั่วคราวชื่อ `AmHerb_Test_<GUID>` แยกจาก AmHerb** และลบเฉพาะฐานที่สร้างเองเมื่อจบ ต้องมีสิทธิ์สร้าง/ลบ test database Browser tests ต้องมี Chrome หรือกำหนด `AMHERB_CHROME` ภาพทดสอบอยู่ `artifacts/screenshots` ผล TRX อยู่ `artifacts/test-results`

```powershell
dotnet ef migrations has-pending-model-changes --project src/AmHerb.Web
dotnet ef migrations script --idempotent --project src/AmHerb.Web --output sql/migrate.sql
```

## CSV Marketplace

แม่แบบ: `src/AmHerb.Web/wwwroot/samples/marketplace-orders.csv` นำเข้าที่ Admin → Imports โดย Finance/Admin

- Channel: Shopee, TikTokShop, Lazada; จับคู่ SKU ด้วยรหัสสินค้า
- หลายแถว ExternalOrderId เดียวกันรวมเป็นหนึ่งคำสั่งซื้อ ข้อมูลลูกค้า ค่าธรรมเนียม ยอดชำระ และสถานะระดับ order ต้องตรงกันทุกแถว
- ยอด fee/affiliate ในแต่ละแถวเป็น **ยอดรวมต่อ order** ไม่ใช่ต่อชิ้น
- ใช้ราคาตาม channel/effective-date ในระบบ; ตั้ง channel price ก่อน import หากราคาต่างจากหน้าร้าน
- FulfillmentStatus: PendingPayment, Paid, Shipped, Delivered ถ้า Paid ขึ้นไปต้องมี TransactionRef/PaidAmount ตรงยอด ถ้าจัดส่งต้องมี Carrier/TrackingNumber
- รองรับ import ซ้ำด้วย Channel + ExternalOrderId และอัปเดต fulfillment โดยไม่สร้างยอดซ้ำ ไม่ทำ retail verification อัตโนมัติ
- แต่ละ order commit แยกกัน หากขั้นถัดไปผิดพลาด order ที่สร้างแล้วคงสถานะล่าสุด สามารถแก้ CSV แล้ว import ซ้ำได้

## API และ deployment

- ตัวอย่าง requests: `docs/api.http`
- สถาปัตยกรรม: `docs/architecture.md`
- IIS: `docs/iis.md`, `scripts/Publish-IIS.ps1` (ถ้าเครื่องบล็อก scripts ใช้ `dotnet publish src/AmHerb.Web -c Release -r win-x64 --self-contained false -o artifacts/publish`)
- ความปลอดภัย/ขอบเขตการเชื่อมต่อ: `docs/security.md`
- ต้นฉบับ requirements: `docs/master-prompt.md`

## ค่าภายนอกที่ยังต้องกำหนดสำหรับใช้งานจริง

SuperAdmin credentials, SMTP, บัญชีธนาคาร/คำแนะนำชำระเงิน, hostname/HTTPS/IIS และข้อมูลสินค้า/สต็อกจริง Google OAuth เป็นตัวเลือก

PromptPay/card payment, Shopee/TikTok/Lazada live APIs, courier APIs และ LINE OA เป็น adapter stubs ตาม Master Prompt ยังไม่รับ webhook จริง (ตอบ 501) ต้องใส่ credentials และพัฒนา signature verification/provider protocol ก่อนเปิดช่องทางเหล่านี้ ระบบที่ใช้งานได้ทันทีหลังตั้งค่าคือ manual bank transfer, cash POS, manual shipping และ CSV marketplace



## รายงานสต๊อกและการแยกคลัง

- เปิด `/Stock` จากเมนูรายงานสต๊อก เลือกบริษัทกลาง ดีลเลอร์/ผู้ขาย หรือสมาชิก แล้วกรองคลัง ชื่อสินค้า SKU ล็อต และสถานะ
- คงคลัง = จำนวนยังไม่จอง + จำนวนจอง; พร้อมขาย = จำนวนยังไม่จองที่ไม่หมดอายุและอยู่ในคลังเปิดใช้ จึงไม่รวมจำนวนจองซ้ำ สินค้าใกล้หมดอายุอยู่ในช่วง 90 วัน
- รายงานแสดงสินค้าเมื่อมีประวัติรับเข้าคลังแล้ว รวมล็อตที่เหลือศูนย์ ไม่สร้างสต๊อกสมมติให้คลังใหม่
- SuperAdmin/Admin/Warehouse/Finance/Marketing เห็นภาพรวมและมูลค่าทุน สมาชิกเห็นเฉพาะคลังที่ตนเป็นเจ้าของและไม่มีข้อมูลต้นทุนในรายงานหรือ CSV
- ผู้ดูแลคลังเพิ่มคลังบริษัท ดีลเลอร์ หรือสมาชิกจากส่วนจัดการคลังในรายงานได้ โดยคลังดีลเลอร์/สมาชิกต้องมีเจ้าของที่ยังใช้งานอยู่
- รับสินค้าและปรับยอดจากหลังบ้านสต๊อก พร้อมเหตุผลใน audit; Admin กำหนดคลังให้ POS หลังปิดกะ โดยเลือกคลังบริษัทหรือคลังของเจ้าของจุดขายเท่านั้น
- การสมัครใหม่สร้างคลังสมาชิกส่วนตัวและผูก POS ทันที คลังเดิมจัดเป็นบริษัทกลางและการผูก POS เดิมยังคงอยู่
- CSV ใช้ตัวกรองและสิทธิ์เดียวกับหน้าจอ รองรับภาษาไทยใน Excel และป้องกันสูตรจากข้อความ ส่วนรายงานพิมพ์ใช้ปุ่มพิมพ์รายงาน
- การแจ้งเตือนสต๊อกคำนวณแยกคลังและส่งให้เจ้าของคลัง ส่วนคลังบริษัทส่งให้หลังบ้าน
- ภาพหน้าจอที่ผ่านการทดสอบ: `artifacts/screenshots/stock-desktop.png`, `artifacts/screenshots/stock-mobile.png`

## เบิกและโอนย้ายสินค้า

เปิด `/Transfers` เพื่อขอเบิกจากบริษัทเข้าคลังส่วนตัว ผู้ดูแลคลังอนุมัติและจ่ายด้วย FEFO จากนั้นเจ้าของปลายทางยืนยันรับครบจำนวน สต๊อกระหว่างขนส่งแสดงแยกใน `/Stock` และจะไม่พร้อมขายจนกว่าจะรับเข้า รองรับใบเบิกหนึ่ง SKU หลายล็อตต่อเอกสาร พร้อมพิมพ์ใบส่งของ เลขติดตาม การยกเลิกก่อนจ่าย แจ้งเตือน และ audit ป้องกันการตัด/รับซ้ำ การเบิกไม่ออกใบเสร็จ ไม่สร้างหนี้ และไม่ให้ Token

รายละเอียดผลวิเคราะห์ ขอบเขต และงานที่ยังต้องกำหนด: [system-gap-analysis](docs/system-gap-analysis.md)

## ภาพสินค้า ข่าว โฆษณา สนทนา และบัญชีขาย

- จัดการภาพจากหน้าแก้ไขสินค้า: ภาพหลัก 4 ช่อง + ภาพข้อมูล 1 ช่อง เก็บ binary ใน SQL Server พร้อมภาพย่อวงกลมและคลิกขยาย
- หน้าแรกและ Login ออกแบบใหม่ด้วยภาพ AM HERB ที่ผู้ใช้ให้มา ภาพแบรนด์เก็บในฐานข้อมูลเช่นกัน
- `/News/Manage`: ข่าว/โฆษณาแบบร่างและกำหนดเวลา แยกทั่วไป/สมาชิก/ทุกคน; `/News` และหน้าสมาชิกแสดงตามสิทธิ์
- `/Messages`: สนทนาสมาชิกกับทีมงาน สถานะรอตอบ/อ่านแล้ว/ปิดเรื่อง พร้อมตัวเลขข้อความใหม่ทุก 30 วินาที
- ปุ่มพิมพ์รายงานเปิดตัวอย่าง modal และหน้าต่างพิมพ์/บันทึก PDF
- `/Accounting`: บัญชีย่อยขายและคืนสินค้า เดบิต–เครดิตสมดุล รายได้/ภาษี/ค่าส่ง/Token discount/ต้นทุน; ไม่ใช่บัญชีทั่วไปครบชุด
- อ่านวิธีใช้ ขอบเขต และการเก็บข้อมูลที่ [คู่มือโมดูลใหม่](docs/media-content-messaging-accounting.md)

## เครดิตสมาชิกและลูกหนี้
ดู [คู่มือเครดิตสมาชิก](docs/member-credit.md) สำหรับวงเงินซื้อเชื่อ การรับโอนชำระหนี้ และรายงานใบแจ้งยอด (/Credit).
