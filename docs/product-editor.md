# Product information and image editor

Open **หลังบ้าน → Products → แก้ไขสินค้า** (`/Admin/Product/{skuId}`). Product details and image management share one page. Existing `/Media/Product/{productId}` links still open the image-only editor.

- Save product fields independently; remain on the editor after saving, including newly created products.
- Four gallery positions plus one separate product information image.
- Local preview, filename, dimensions and file size before upload. JPEG/PNG/WebP, maximum 5 MB. Server validation remains authoritative.
- Edit image captions without uploading the original again.
- Select position 1 to make an image primary; swapping occupied positions preserves both images. Empty target positions move the existing image. The lowest occupied gallery position is primary.
- Delete requires confirmation; warn when an image action would discard unsaved product details.
- Public product preview link opens the storefront in another tab.
- Images remain binary data in SQL Server. Only authorized Prices-policy staff can mutate them; every change is audited and POST actions require antiforgery tokens.
- Price/token fields are read-only for existing SKUs: their versioned management pages handle changes.

Validation: SQL integration tests cover occupied/empty position moves, separate information image protection, caption changes and unchanged image bytes. Browser checks cover local preview, integrated product save, caption save, position change and mobile overflow.
