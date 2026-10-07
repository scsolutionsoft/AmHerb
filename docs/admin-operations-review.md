# Administrative operations review

The former admin landing page was a sales report; orders were a flat list, member links did not lead to a unified review, and stock, accounting and credit required separate navigation. `/Admin` now opens `/Operations`, a permission-aware work queue. Existing financial and inventory transaction services remain authoritative.

## Operations and classification

- `/Operations/Orders`: date range in Bangkok time, source, member relationship, order status, payment status and shipping queue; 50 rows per page with filter-preserving pagination. Counts and payable totals cover the entire filtered result, not just the current page. Payable includes selected statuses and is deliberately not labelled revenue or cash collected.
- Source classification separates central Store/Manual orders, member online stores, member-warehouse POS, central-warehouse POS, and marketplaces. The central option includes central POS; these are overlapping filter views, not additive accounting buckets. POS uses the warehouses of allocated inventory lots, not the register's current assignment. An order with any member-owned allocation is member-warehouse POS; POS without member allocations falls into central POS. These are operational source labels, not a determination of who holds money.
- Member sales include the seller/referrer, storefront owner and POS cashier. A single order is returned once even when a member fills multiple roles. Buyer, current downline, current upline, and actual historical Token-recipient filters are separate. Moving a sponsor changes current network views but not historical Token distributions.
- Staff can prepare, dispatch, mark delivered and print A5 labels from the shipping queue. Operations uses existing serializable fulfillment services and audit records. No shipping mutations are allowed for POS through these actions; unpaid orders cannot be dispatched. Company sender details use `Shipping:SenderPhone` and `Shipping:SenderAddress` when configured, with print-preview correction available.

## Financial and member review

- `/Operations/Member/{id}` shows current ancestors and paged descendants, linked order views, and permission-gated credit and Token balances. Token entries link to source orders when the viewer has Orders permission.
- `/Operations/Tokens` searches members, displays ledger-derived balances, and filters negative available balances. Existing adjustment and policy screens remain linked.
- `/Operations/Clearing` shows credit debt as Amount minus Paid minus Adjusted, overdue debt, and pending receipt counts across all filtered accounts. Each account links to the existing receipt-review/allocation workflow. Merely viewing an upload never clears debt.
- POS cash differences are a separate list of the latest 30 matching closed sessions. They are not combined with member credit or Token balances. The existing domain has no resolution status for POS discrepancies; this release does not invent a settlement or automatically close a discrepancy.
- `/Accounting` filters journal entries by the same source classification and optionally an involved member (buyer/seller/store owner/cashier). Each journal links to the order. Accounting periods remain capped at 366 days.

Permissions: all operations require BackOffice; order/shipping/labels additionally require Orders, clearing and accounting require Payments, Token lists require Tokens. Member review requires Members, Payments or Tokens and only displays finance/Token panels when their own policy is granted. Ordinary members cannot use these admin endpoints. The hub/nav show only permitted modules.

No schema changes are introduced. Existing migrations must remain applied; no direct balance or status rewriting is used. Validation includes SQL classification tests, historical distribution after sponsor moves, browser routes, staff versus ordinary-member access, real fulfillment actions, filtered credit totals, and mobile layouts.

## Stock adjustment popup

Inventory-authorized staff can select Adjust stock beside a product on `/Stock`, select an individual lot, or open the same popup from `/Admin?section=Inventory`. The popup fetches current lot balances and row version, shows the proposed unreserved and total quantities, requires a reason, and posts through InventoryService. Reserved stock is left unchanged. A stale version or failed submission requires reloading current data before retrying; successful saves reload the current filtered page. Changes are recorded in inventory transactions and the audit log. No schema change is needed.

## Guided transfer flow

`/Transfers` now guides warehouse/product selection, review of both warehouse balances and requested quantity, and the request reason. Product/lot dialogs show quantities without exposing inventory costs. Request, dispatch, receive and cancel have confirmation dialogs; receiving additionally requires a completed-count checkbox. Successful actions display a notification popup. Existing notifications and audit records are retained.

The preview endpoint authorizes a new request against the same source/destination relationship rules, or scopes existing transfers through Visible before deriving warehouse and SKU IDs. It returns on-hand, reserved, eligible unreserved, unavailable, inbound dispatched and outgoing requested quantities. Lot detail is capped at 100 rows, while totals include all lots. Existing transfer detail refreshes quantities every 15 seconds while visible and no modal is open. Confirmations fetch fresh balances; dispatch still performs its own authoritative check inside the serializable transaction.

Sales reservations already reduce QtyAvailable. Transfer dispatch consumes this remainder and never subtracts reservations again. Pending transfer requests do not reserve inventory. A request may await replenishment, but dispatch cannot exceed the current transferable quantity. Source is deducted on dispatch; destination is increased only on receipt. Expired stock is excluded from transferable quantity; goods that expire in transit can be received but remain unavailable for sale. Disabled products cannot be dispatched and inactive destination owners cannot receive. This workflow neither creates sales revenue nor issues Token. No schema change is introduced.
