using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.Services;

public record ThaiAddress(string Id, string Name, string Parent, int Level, string[] Postcodes);
public static class ThaiAddresses
{
    private static readonly Lazy<Dictionary<string, ThaiAddress>> Data = new(() => {
        using var stream = typeof(ThaiAddresses).Assembly.GetManifestResourceStream("AmHerb.Web.Data.Addresses.thailand.json")
            ?? throw new InvalidOperationException("Missing Thailand address dataset");
        return JsonSerializer.Deserialize<List<ThaiAddress>>(stream)!.ToDictionary(x => x.Id);
    });
    public static IEnumerable<ThaiAddress> Children(string? parent) => Data.Value.Values.Where(x => string.IsNullOrEmpty(parent) ? x.Level == 1 : x.Parent == parent).OrderBy(x => x.Name);
    public static string Format(CheckoutInput input)
    {
        if (string.IsNullOrWhiteSpace(input.HouseNumber) || input.HouseNumber.Length > 100 ||
            !Data.Value.TryGetValue(input.SubdistrictCode ?? "", out var sub) || sub.Level != 3 ||
            !Regex.IsMatch(input.PostalCode ?? "", "^[0-9]{5}$") || !sub.Postcodes.Contains(input.PostalCode))
            throw new BusinessException("กรุณากรอกบ้านเลขที่ เลือกจังหวัด อำเภอ ตำบล และรหัสไปรษณีย์ให้ครบ");
        if (!string.IsNullOrEmpty(input.VillageCode))
        {
            if (!Data.Value.TryGetValue(input.VillageCode, out var village) || village.Level != 4 || village.Parent != sub.Id)
                throw new BusinessException("หมู่บ้านไม่ตรงกับตำบลที่เลือก");
            input.VillageName = village.Name;
        }
        var district = Data.Value[sub.Parent]; var province = Data.Value[district.Parent];
        return string.Join(" ", new[] { input.HouseNumber.Trim(), input.VillageName?.Trim(), input.AddressExtra?.Trim(), sub.Name, district.Name, province.Name, input.PostalCode }.Where(x => !string.IsNullOrEmpty(x)));
    }
}

public record CarrierChoice(long Id, string Name, bool Enabled);
public class DeliveryService(AmHerbDbContext db)
{
    public async Task<List<CarrierChoice>> Options(long? storeId)
    {
        var providers = await db.ShippingProviders.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        var settings = storeId == null ? new Dictionary<long, bool>() : await db.StoreShippingOptions.Where(x => x.StoreId == storeId).ToDictionaryAsync(x => x.ShippingProviderId, x => x.Enabled);
        return providers.Select(x => new CarrierChoice(x.Id, x.Name, settings.GetValueOrDefault(x.Id, x.Id <= 4))).ToList();
    }
    public static string Normalize(string name)
    {
        var key = Regex.Replace(name.Normalize(NormalizationForm.FormKC).ToUpperInvariant(), @"[\s\p{P}\p{S}]", "");
        return key switch {
            "ไปรษณีย์" or "ไปรษณีย์ไทย" or "THAILANDPOST" or "THAIPOST" => "THAILANDPOST",
            "เคอร์รี่" or "เคอรี่" or "KERRY" or "KERRYEXPRESS" or "KEXEXPRESS" or "เคอร์รี่เอ็กซ์เพรส" or "KEX" or "KERRYKEX" => "KEX",
            "แฟลช" or "เฟลช" or "เฟลชเอ็กซ์เพรส" or "แฟลชเอ็กซ์เพรส" or "FLASH" or "FLASHEXPRESS" => "FLASH",
            "JT" or "JTEXPRESS" or "เจแอนด์ที" => "JT", _ => key
        };
    }
    public async Task Set(long storeId, long providerId, bool enabled)
    {
        if (!await db.ShippingProviders.AnyAsync(x => x.Id == providerId)) throw new BusinessException("ไม่พบผู้ขนส่ง");
        var option = await db.StoreShippingOptions.SingleOrDefaultAsync(x => x.StoreId == storeId && x.ShippingProviderId == providerId);
        if (option == null) { option = new StoreShippingOption { StoreId = storeId, ShippingProviderId = providerId }; db.StoreShippingOptions.Add(option); }
        option.Enabled = enabled; await db.SaveChangesAsync();
    }
    public async Task Add(long storeId, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new BusinessException("ระบุชื่อขนส่งไม่เกิน 120 ตัวอักษร");
        var key = Normalize(name);
        if (key.Length == 0 || await db.ShippingProviders.AnyAsync(x => x.NormalizedName == key)) throw new BusinessException("มีชื่อผู้ขนส่งนี้แล้ว กรุณาเลือกเปิดใช้งานจากรายการ");
        var provider = new ShippingProvider { Name = name.Trim(), NormalizedName = key };
        db.StoreShippingOptions.Add(new StoreShippingOption { StoreId = storeId, ShippingProvider = provider, Enabled = true });
        await db.SaveChangesAsync();
    }
}
