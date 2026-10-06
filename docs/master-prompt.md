# AM HERB Member Commerce & Reward Network — Codex Master Prompt

You are a senior ASP.NET Core / SQL Server architect and full-stack engineer. Build a production-ready commerce, membership, referral network, inventory, and closed-loop reward platform for AM HERB.

## 1. Technology and delivery constraints
- ASP.NET Core MVC on .NET 8.
- SQL Server 2019+ with EF Core 8 Code First migrations.
- ASP.NET Core Identity for authentication.
- Razor Views + Tag Helpers + Bootstrap 5.3 only for the web UI.
- Clean, professional responsive UI; green/gold/white AM HERB theme.
- REST API endpoints for future Flutter/mobile and marketplace integration.
- Target deployment: Windows Server 2019/2022 IIS x64.
- Use UTC in database timestamps; display Asia/Bangkok in UI.
- Thai/English localization ready.
- Do not hardcode secrets, API keys, passwords, connection strings, or payment credentials.
- Generate all source files, migrations, seeders, views, controllers, services, validation, tests, README, IIS publish instructions, and SQL scripts.
- Do not leave TODOs for core business flows. Stubs are allowed only for third-party marketplace/payment APIs that require external credentials.

## 2. Core business concept
AM HERB sells dietary supplement / herbal products through:
1) AM HERB own online store,
2) member/reseller referral links and QR codes,
3) Shopee,
4) TikTok Shop,
5) Lazada,
6) admin/manual orders.

Members can have one Sponsor/Upline and unlimited Downlines. Network rewards must arise from verified retail product sales only. Do not reward recruitment, membership signup, or stock-loading purchases.

AM Token is a closed-loop reward credit:
- non-cash,
- non-transferable between members,
- not tradable outside AM HERB,
- no interest / appreciation,
- redeemable only for eligible AM HERB merchandise,
- default redemption reference: 1 AM Token = 1 THB of eligible merchandise value inside the system,
- shipping, taxes, and non-eligible charges are paid by normal payment methods,
- rules must be admin-configurable and versioned by effective date.

## 3. Reward rules
Default sales network distribution:
- Seller: 100% of SKU Base Token
- Upline Level 1: 20%
- Upline Level 2: 7%
- Upline Level 3: 3%
- max network depth for rewards: 3 levels

Rewards are not immediately available:
- On verified fulfilled retail sale: create Token distributions with status Pending.
- Default pending period: 14 days (admin configurable 0–60).
- Scheduled job releases eligible Pending Token to Available.
- Refund/return/cancel must reverse all related Token across Seller and Upline levels using compensating ledger entries, never deleting ledger history.
- Self-purchase cannot generate network commission. It may optionally generate Loyalty Token if that feature is enabled.
- External marketplace affiliate orders must support a channel policy that can disable network commission to prevent double commission.

## 4. Initial product and token seed data
Use the following as initial admin-editable data. All rates must be versioned and never hardcoded in calculation code.

| Product | Retail | Intro/Promo | Wholesale 1 | Pack 6 total | Pack 12 total | Base Token |
|---|---:|---:|---:|---:|---:|---:|
| FOREVA | 790 | 690 | 495 | 2821.50 | 5400 | 60 |
| CELL-SYNC | 990 | 890 | 605 | 3448.50 | 6600 | 90 |
| The Ruby | 1490 | 1290 | 935 | 5329.50 | 10200 | 110 |
| Detoxify Blue | 1090 | NULL | NULL | NULL | NULL | 80 |
| PHYTOSYNC | 690 | 590 | 418 | 2382.60 | 4560 | 50 |
| Anti Neo Plus | 1990 | 1790 | 1320 | 7500 | 14640 | 140 |
| Reliva | 249 | 229 | 161 | 966 | 1932 | 20 |
| Reliva Max | 299 | 279 | 195.50 | 1173 | 2346 | 25 |
| FEEL GOOD CHAMANG | 299 | 249 | 180 | 1020 | 1980 | 20 |

Important: Detoxify Blue older wholesale pricing is obsolete/inconsistent with the current retail price. Keep wholesale/pack prices blank until admin enters confirmed values.

## 5. Required modules
### A. Authentication and security
- Identity registration/login/logout/password reset/email confirmation.
- Optional Google OAuth behind configuration.
- Roles: SuperAdmin, Admin, Finance, Warehouse, CustomerService, Marketing, Member.
- Fine-grained policies for price management, token adjustments, refunds, reports, inventory, member network, settings.
- MFA-ready.
- Anti-forgery, secure cookies, password policy, lockout, rate limiting for APIs.
- Full audit log for privileged actions.

### B. Member management
- Member code generated uniquely.
- Personal profile and contact data.
- SponsorMemberId self-reference.
- ReferralCode and QR referral URL.
- Member status: Pending, Active, Suspended, Closed.
- Member type: Customer, Member, Reseller, Leader (titles are operational only; never pay on recruitment).
- Upline breadcrumb and Downline tree.
- Prevent cycles in sponsor graph.
- Sponsor changes require authorized admin workflow and audit trail.
- Member profile should show personal sales, team sales, direct downlines, active downlines, token balance, pending token, redemptions, orders.

### C. Member hierarchy
Use both:
- Members.SponsorMemberId for direct parent,
- MemberClosure table (AncestorMemberId, DescendantMemberId, Depth) for fast hierarchy queries.
Maintain closure data transactionally when creating or moving members.
Reward engine reads only first 3 ancestors at sale time and stores snapshot recipients in TokenDistribution so later sponsor changes do not rewrite past payouts.

### D. Product catalog
- Product, SKU, category, images, descriptions, status.
- Variant support (capsule count, weight, packaging).
- Retail price, member/wholesale tiers, pack quantities.
- Effective-dated ProductPrice table.
- Cost/COGS field visible only to authorized roles.
- Base Token per SKU in effective-dated TokenRate table.
- Channel-specific price/token overrides.
- Regulatory metadata: food serial/registration number, manufacturer, distributor, lot requirements, warnings, label documents.

### E. Shopping cart and orders
- Guest/customer/member checkout.
- Cart, address, shipping, coupon, payment method.
- Order states: Draft, PendingPayment, Paid, Processing, Shipped, Delivered, Completed, Cancelled, ReturnRequested, Returned, Refunded.
- Every order records SalesChannel and attribution source.
- Retail attribution supports MemberId, referral code, campaign, creator, marketplace order id.
- Order totals: merchandise, discount, shipping, tax if applicable, grand total, token redemption, cash payable.
- Idempotent payment confirmation and webhook processing.

### F. Payments
Create provider abstraction:
- IPaymentGateway
- ManualBankTransferProvider
- PromptPayQrProvider stub
- CardGatewayProvider stub
Store payment intent, transaction ref, amount, status, timestamps, raw webhook hash/reference (not secrets).

### G. Marketplace integration
Create IMarketplaceProvider with adapters:
- ShopeeProvider stub
- TikTokShopProvider stub
- LazadaProvider stub
- CsvMarketplaceImportProvider fully functional
Capabilities: import orders, update fulfillment status, map SKU, map external order ids, channel fees, affiliate fee, campaign data.
MarketplaceChannelPolicy controls whether an external order can create network Token.

### H. Referral links and QR
- Each member gets shareable referral URL/QR.
- Attribution cookie with configurable expiry.
- Last-click default; architecture allows first-click later.
- Record ReferralVisit, source, campaign, landing page, converted order.
- Prevent attribution to suspended members.

### I. Reward Token engine
Entities:
- TokenPolicy
- TokenRate
- TokenDistribution
- TokenLedger
- TokenBalanceSnapshot (optional cached aggregate)

Ledger transaction types:
SALE_REWARD, UPLINE_L1, UPLINE_L2, UPLINE_L3, LOYALTY, PROMOTION, RELEASE, REDEMPTION, RETURN_REVERSAL, EXPIRY, ADMIN_ADJUSTMENT.

Statuses:
Pending, Available, Reserved, Redeemed, Reversed, Expired.

Rules:
- decimal(18,2) for token amounts.
- Ledger is append-only.
- Never edit/delete a posted ledger record; use reversal/adjustment entries.
- Every reward references SourceOrderId and OrderItemId where applicable.
- Save RewardPolicyVersion and TokenRateVersion used for calculation.
- Use DB transaction for order completion + distribution creation.
- Scheduled release and expiry jobs must be idempotent.

### J. Token redemption
- Member can apply available Token at checkout subject to policy.
- Reserve Token while order payment is pending.
- Release reservation on failed/cancelled checkout.
- Consume Token after order confirmation.
- Redemption can be limited by product/channel/category.
- Default max merchandise redemption = 100%, admin configurable.
- Shipping not redeemable by default.
- Provide redemption history and statement.

### K. Returns/refunds
- Return request workflow and approval.
- Refund amount calculation.
- Restore stock if sellable.
- Reverse all reward distributions linked to returned quantity.
- Partial return must reverse proportionally by item quantity.
- If member already spent Token that is later reversed, allow negative Available balance and block new redemptions until recovered, with admin review flag.

### L. Inventory and lot tracking
- Warehouses.
- InventoryBatch: SKU, LotNo, MfgDate, ExpDate, QtyReceived, QtyAvailable, Cost.
- FEFO allocation by expiry date.
- InventoryTransaction types: Receive, Sale, Reserve, Release, Return, Adjust, Damage, Expire.
- Low stock alerts and expiry alerts 30/60/90 days.
- Stock card report.

### M. Shipping
- Shipment entity, carrier, tracking number, status.
- Shipping fee rules.
- Provider abstraction for future courier APIs.
- Manual fulfillment page and printable packing list.

### N. Promotions and coupons
- Fixed, percentage, bundle, free-gift, channel-specific promotions.
- Effective dates, usage limits, member eligibility.
- Guard against stacking rules.
- Promotion can optionally modify Base Token; must be explicit, versioned, auditable.

### O. Dashboard and reports
Admin dashboard:
- GMV, Net Sales, Orders, AOV, new/repeat customers.
- Sales by channel / SKU / campaign.
- Gross margin and contribution margin if COGS and fees are available.
- Active members, active sellers, downline growth.
- Personal/team retail volume.
- Token issued, pending, available, redeemed, reversed, expired.
- Token liability in redemption-equivalent THB.
- Upline payout by level.
- Marketplace fees and affiliate fees.
- Inventory, low stock, near-expiry.

Member dashboard:
- personal orders/sales,
- team retail sales,
- direct downlines,
- tree view,
- token pending/available,
- statement,
- redemption,
- referral link/QR,
- product catalog/member prices.

Reports must export CSV and Excel-friendly CSV; PDF export interface may be added later.

### P. Compliance guardrails
Implement non-bypassable business rules:
- no reward for signup,
- no reward for sponsor creation,
- no reward for membership fee,
- no network reward for self-purchase,
- no automatic reward merely for buying 6/12-pack inventory,
- network reward only when sale is marked VerifiedRetailSale,
- refund reverses rewards,
- closed-loop token only,
- no cash withdrawal,
- no peer-to-peer token transfer,
- no external exchange/trading.
Admin UI must display warnings before changing reward rules and require reason + audit log.

### Q. Notifications
Create notification abstraction and in-app notifications for:
- order status,
- token pending/released/reversed,
- new direct downline,
- low stock,
- expiry warnings,
- refund/return updates.
Email provider can use SMTP; LINE OA integration should be a future adapter stub.

### R. Background jobs
Use BackgroundService initially; design interface compatible with Hangfire later.
Jobs:
- release pending tokens,
- expire tokens,
- inventory expiry alerts,
- marketplace import sync,
- cleanup abandoned carts,
- report snapshot aggregation.

## 6. Database requirements
Use SQL Server-friendly schema with:
- bigint identities or GUIDs consistently; prefer bigint for transactional tables and GUID public IDs where useful.
- datetime2 UTC.
- decimal(18,2) for money and token.
- rowversion for concurrency on stock/pricing/member records.
- indexes on SponsorMemberId, closure ancestor/descendant, OrderNumber, ExternalOrderId, ReferralCode, TokenLedger(MemberId, PostedAt), InventoryBatch(SkuId, ExpDate).
- foreign keys with conservative delete behavior; transactional records must not cascade-delete.

## 7. Required pages
Public:
- Home, Product list/detail, Cart, Checkout, Order tracking, Login/Register.
Member:
- Dashboard, Profile, Referral/QR, Downline Tree, Orders, Sales, Wallet/Token Statement, Redeem, Product/member pricing.
Admin:
- Dashboard, Members, Sponsor Tree, Products, Prices, Token Rates/Policies, Orders, Payments, Marketplace Imports, Inventory/Lots, Returns, Promotions, Reports, Audit Logs, Settings.

## 8. API endpoints
Create versioned /api/v1 endpoints for:
- auth/profile,
- products/prices,
- cart/order,
- member tree summary,
- token balance/ledger,
- redemption quote,
- referral attribution,
- marketplace webhook placeholders.
Use DTOs; never expose EF entities directly.

## 9. Calculation examples / acceptance tests
### CELL-SYNC
Base Token = 90
Retail sale by Member D with ancestors C/B/A:
- D seller: 90.00 Pending
- C L1: 18.00 Pending
- B L2: 6.30 Pending
- A L3: 2.70 Pending
Total = 117.00
After 14 days and no return -> Available.
If full return -> append reversals totaling -117.00 to the same recipients.

### FOREVA
Base Token = 60
- Seller 60.00
- L1 12.00
- L2 4.20
- L3 1.80
Total 78.00

### Detoxify Blue
Base Token = 80
- Seller 80.00
- L1 16.00
- L2 5.60
- L3 2.40
Total 104.00

### Self-purchase
Member buys for their own account:
- network distributions = 0
- loyalty = only if enabled by TokenPolicy.

### Marketplace affiliate
If MarketplaceChannelPolicy.AllowNetworkReward = false:
- network distributions = 0 even when referral member exists.

## 10. UI design
- Thai first, English ready.
- Desktop/mobile responsive.
- Dashboard cards, clean tables, filters, date ranges.
- Upline/downline tree with expandable nodes.
- Wallet statement must visually separate Pending and Available.
- Color-coded order/token statuses.
- Use accessible contrast and standard Bootstrap components.

## 11. Seed roles and settings
Seed roles only; do NOT seed a known admin password.
Admin bootstrap account must be created from environment variables on first run:
AMHERB_ADMIN_EMAIL
AMHERB_ADMIN_PASSWORD

Seed SystemSettings:
RewardPendingDays=14
MaxUplineRewardDepth=3
TokenRedemptionReferenceTHB=1.00
TokenTransferEnabled=false
TokenCashWithdrawalEnabled=false
TokenExpiryMonths=12
MaxTokenRedemptionPercent=100
ShippingRedeemableWithToken=false

## 12. Testing requirements
Unit tests:
- reward calculation,
- 3-level upline distribution,
- self-purchase exclusion,
- return reversal,
- partial return reversal,
- token reservation/redemption,
- sponsor cycle prevention,
- effective-dated price/token rates.
Integration tests:
- order paid -> retail verification -> reward pending,
- pending release job,
- redemption checkout,
- refund with spent-token negative balance,
- inventory lot allocation.

## 13. Deliverables
Generate:
1. Complete solution structure.
2. Domain entities and enums.
3. EF DbContext and migrations.
4. Services/interfaces.
5. Controllers and Razor Views.
6. API controllers and DTOs.
7. Seed data for products/prices/token rates above.
8. SQL Server scripts.
9. Tests.
10. README with setup, migration, IIS publish steps.
11. appsettings examples.
12. Security and compliance notes.
13. Sample CSV import template for marketplace orders.
14. Postman/HTTP request examples.
15. Architecture diagram in Mermaid inside docs/architecture.md.

## 14. Implementation order
Build in this order and keep the solution runnable at the end of each phase:
Phase 1: Identity, Members, Products, Prices, Orders, Inventory.
Phase 2: Referral attribution, hierarchy closure, Token engine, wallet, redemption.
Phase 3: Returns/refunds, promotions, marketplace CSV import, dashboards/reports.
Phase 4: API, background jobs, marketplace/payment provider stubs, notifications, hardening, tests.

At the end, run static validation, ensure migrations are consistent, and list any external credentials/configuration still required. Do not change the reward percentages or product Base Token seed values unless explicitly instructed.
