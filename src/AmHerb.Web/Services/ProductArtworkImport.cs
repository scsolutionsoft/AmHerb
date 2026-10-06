using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;

// Original attachments from 6 October 2026. Match SKU codes, never database IDs.
public static class ProductArtworkImport
{
    public static readonly (int File, string Code, int Slot)[] Mapping =
    [
        (1, "CELL-SYNC", 1), (14, "CELL-SYNC", 5),
        (2, "RELIVA-MAX", 1), (3, "PHYTOSYNC", 2),
        (13, "PHYTOSYNC", 1), (10, "PHYTOSYNC", 5),
        (4, "ANTI-NEO-PLUS", 1), (5, "RELIVA", 1),
        (12, "DETOXIFY-BLUE", 1), (6, "DETOXIFY-BLUE", 2), (9, "DETOXIFY-BLUE", 5),
        (7, "FOREVA", 1), (11, "FOREVA", 5), (8, "RUBY", 1)
    ];

    public static async Task RunAsync(AmHerbDbContext db, AuditService audit, string directory)
    {
        var files = new Dictionary<int, (byte[] Data, string Type)>();
        for (var i = 1; i <= 15; i++)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, $"product-{i:00}.jpg"));
            files.Add(i, (bytes, MediaService.Validate(bytes)));
        }
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var codes = Mapping.Select(x => x.Code).Distinct().ToArray();
        var skus = await db.Skus.Include(x => x.Product).Where(x => codes.Contains(x.Code)).ToDictionaryAsync(x => x.Code);
        if (skus.Count != codes.Length) throw new InvalidOperationException("Import cancelled: a mapped SKU is missing.");
        foreach (var item in Mapping)
        {
            var sku = skus[item.Code];
            var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.ProductId == sku.ProductId && x.Slot == item.Slot);
            if (asset == null) { asset = new MediaAsset { ProductId = sku.ProductId, Slot = item.Slot }; db.MediaAssets.Add(asset); }
            asset.Data = files[item.File].Data; asset.ContentType = files[item.File].Type;
            asset.Alt = sku.Product.Name + (item.Slot == 5 ? " — รายละเอียดผลิตภัณฑ์" : " — ภาพผลิตภัณฑ์");
            asset.UpdatedAt = DateTime.UtcNow;
        }
        const string title = "ร่าง: แผนสมาชิก AM HERB — ตรวจสอบข้อมูล Token";
        var post = await db.ContentPosts.Include(x => x.MediaAsset).SingleOrDefaultAsync(x => x.Title == title);
        if (post == null)
        {
            post = new ContentPost { Title = title, Audience = ContentAudience.Members, Kind = ContentKind.News,
                StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddYears(1),
                Body = "ภาพแผนสมาชิกที่ได้รับวันที่ 6 ตุลาคม 2026 สำหรับตรวจสอบก่อนเผยแพร่: ภาพระบุ Detoxify Blue 210 Token แต่ค่าที่กำหนดในระบบขณะนำเข้าคือ 80 Token กรุณาตรวจสอบราคาและเงื่อนไขทั้งหมดกับข้อมูลปัจจุบันก่อนเผยแพร่",
                MediaAsset = new MediaAsset { Purpose = "content", Alt = "ภาพอ้างอิงแผนสมาชิก AM HERB — รอตรวจสอบ" } };
            db.ContentPosts.Add(post);
        }
        post.Published = false;
        post.MediaAsset ??= new MediaAsset { Purpose = "content", Alt = title };
        post.MediaAsset.Data = files[15].Data; post.MediaAsset.ContentType = files[15].Type;
        post.MediaAsset.UpdatedAt = DateTime.UtcNow;
        audit.Add("Media.ProductArtworkImport", "2026-10-06", "14 originals mapped by SKU; membership infographic saved as draft; pricing and rewards unchanged.");
        await db.SaveChangesAsync();
        // Read back persisted bytes before committing the complete batch.
        foreach (var item in Mapping)
        {
            var productId = skus[item.Code].ProductId;
            var bytes = await db.MediaAssets.AsNoTracking().Where(x => x.ProductId == productId && x.Slot == item.Slot).Select(x => x.Data).SingleAsync();
            if (!bytes.SequenceEqual(files[item.File].Data)) throw new InvalidOperationException("Artwork verification failed.");
        }
        await tx.CommitAsync();
    }
}
