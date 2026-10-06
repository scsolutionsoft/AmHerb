# Product artwork — 6 October 2026

Original user attachments are stored byte-for-byte in SQL Server MediaAssets.Data (varbinary), never assigned by numeric product IDs. Slot 1 is the card/thumbnail, slots 2–4 are additional gallery images, and slot 5 is the product information sheet. Files remain in artifacts/product-import for repeatable import.

| Attachment | SKU | Slot |
|---|---|---|
| 01 | CELL-SYNC | 1 |
| 14 | CELL-SYNC | 5 |
| 02 | RELIVA-MAX | 1 |
| 03 | PHYTOSYNC | 2 |
| 13 | PHYTOSYNC | 1 |
| 10 | PHYTOSYNC | 5 |
| 04 | ANTI-NEO-PLUS | 1 |
| 05 | RELIVA | 1 |
| 12 | DETOXIFY-BLUE | 1 |
| 06 | DETOXIFY-BLUE | 2 |
| 09 | DETOXIFY-BLUE | 5 |
| 07 | FOREVA | 1 |
| 11 | FOREVA | 5 |
| 08 | RUBY | 1 |
| 15 | Member plan | Draft member news, not a product |

FEEL-GOOD has no matching supplied image and retains its existing image/fallback.

The member plan is unpublished: it displays Detoxify Blue 210 Token, whereas the configured rate at import is 80. Review all prices and terms before publishing it. Import does not change prices, stock or reward settings.

Import command (explicit operator command, never runs on startup):

```powershell
dotnet run --project src/AmHerb.Web --launch-profile http -- --import-products C:\path\to\product-import
```

Validates all 15 files, checks exact SKU matches, upserts the mapped slots in one serializable transaction, reads back and compares product bytes before commit. Re-running does not create duplicate images/posts. The member plan remains a draft on re-import. Manage later changes through the product image management page.

Layout uses self-hosted Noto Sans Thai, green/cream colors, uncropped artwork, a responsive three-column product collection and full-image popup previews. Brand photos remain in the lower home-page story section.

Font source: https://github.com/google/fonts/tree/main/ofl/notosansthai
SIL Open Font License included at wwwroot/fonts/OFL-NotoSansThai.txt.
