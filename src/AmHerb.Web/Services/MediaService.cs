using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;
public class MediaService(AmHerbDbContext db, AuditService audit)
{
    public async Task EditCaption(long id, string alt)
    {
        if (alt.Length > 200) throw new BusinessException("คำอธิบายภาพไม่เกิน 200 ตัวอักษร");
        var image = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id && x.ProductId != null)
            ?? throw new BusinessException("ไม่พบภาพสินค้า");
        image.Alt = alt.Trim(); image.UpdatedAt = DateTime.UtcNow;
        audit.Add("Product.ImageCaption", id, image.Alt); await db.SaveChangesAsync();
    }
    public async Task Move(long id, int slot)
    {
        if (slot < 1 || slot > 4) throw new BusinessException("เลือกตำแหน่งภาพสินค้า 1–4");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var source = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id && x.ProductId != null && x.Slot <= 4)
            ?? throw new BusinessException("ไม่พบภาพสินค้า หรือเป็นภาพอธิบาย");
        if (source.Slot == slot) return;
        var target = await db.MediaAssets.SingleOrDefaultAsync(x => x.ProductId == source.ProductId && x.Slot == slot);
        if (target == null) source.Slot = slot;
        else
        {
            // Swap contents, avoiding transient violations of the unique product/slot index.
            (source.Data, target.Data) = (target.Data, source.Data);
            (source.Alt, target.Alt) = (target.Alt, source.Alt);
            (source.ContentType, target.ContentType) = (target.ContentType, source.ContentType);
            target.UpdatedAt = DateTime.UtcNow;
        }
        source.UpdatedAt = DateTime.UtcNow;
        audit.Add("Product.ImageMove", source.ProductId!.Value, $"Image {id} to slot {slot}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public static string Validate(byte[] data)
    {
        if (data.Length < 24 || data.Length > 5 * 1024 * 1024) throw new BusinessException("ภาพต้องมีขนาดไม่เกิน 5 MB");
        if (data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff && data[^2] == 0xff && data[^1] == 0xd9) return "image/jpeg";
        if (data.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return "image/png";
        if (System.Text.Encoding.ASCII.GetString(data,0,4) == "RIFF" && System.Text.Encoding.ASCII.GetString(data,8,4) == "WEBP") return "image/webp";
        throw new BusinessException("รองรับเฉพาะไฟล์ภาพ JPEG, PNG และ WebP");
    }
    public static async Task<byte[]> Read(IFormFile file)
    {
        if(file.Length > 5*1024*1024) throw new BusinessException("ภาพต้องมีขนาดไม่เกิน 5 MB");
        using var stream = new MemoryStream(); await file.CopyToAsync(stream); var bytes = stream.ToArray(); Validate(bytes); return bytes;
    }
    public async Task SaveProduct(long productId, int slot, byte[] bytes, string alt)
    {
        var type = Validate(bytes);
        if(slot < 1 || slot > 5 || alt.Length > 200 || !await db.Products.AnyAsync(x=>x.Id==productId)) throw new BusinessException("สินค้า ช่องภาพ หรือคำอธิบายไม่ถูกต้อง");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var media = await db.MediaAssets.SingleOrDefaultAsync(x=>x.ProductId==productId&&x.Slot==slot);
        if(media==null) { media=new MediaAsset {ProductId=productId,Slot=slot}; db.MediaAssets.Add(media); }
        media.Data=bytes; media.ContentType=type; media.Alt=alt; media.UpdatedAt=DateTime.UtcNow;
        audit.Add("Product.Image", productId, $"Replace image slot {slot}"); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
