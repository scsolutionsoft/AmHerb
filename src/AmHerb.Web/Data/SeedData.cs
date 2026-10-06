using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AmHerb.Web.Data;

public static class SeedData
{
    public static readonly string[] Roles = ["SuperAdmin", "Admin", "Finance", "Warehouse", "CustomerService", "Marketing", "Member"];
    public static void Configure(ModelBuilder b)
    {
        var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        (string Name, string Code, decimal Retail, decimal? Promo, decimal? Wholesale, decimal? Pack6, decimal? Pack12, decimal Token)[] rows =
        [
            ("FOREVA", "FOREVA", 790, 690, 495, 2821.50m, 5400, 60),
            ("CELL-SYNC", "CELL-SYNC", 990, 890, 605, 3448.50m, 6600, 90),
            ("The Ruby", "RUBY", 1490, 1290, 935, 5329.50m, 10200, 110),
            ("Detoxify Blue", "DETOXIFY-BLUE", 1090, null, null, null, null, 80),
            ("PHYTOSYNC", "PHYTOSYNC", 690, 590, 418, 2382.60m, 4560, 50),
            ("Anti Neo Plus", "ANTI-NEO-PLUS", 1990, 1790, 1320, 7500, 14640, 140),
            ("Reliva", "RELIVA", 249, 229, 161, 966, 1932, 20),
            ("Reliva Max", "RELIVA-MAX", 299, 279, 195.50m, 1173, 2346, 25),
            ("FEEL GOOD CHAMANG", "FEEL-GOOD", 299, 249, 180, 1020, 1980, 20)
        ];
        long priceId = 1;
        for (var i = 0; i < rows.Length; i++)
        {
            var r = rows[i]; long id = i + 1;
            b.Entity<Product>().HasData(new Product { Id = id, Name = r.Name });
            b.Entity<Sku>().HasData(new Sku { Id = id, ProductId = id, Code = r.Code, Barcode = r.Code });
            b.Entity<TokenRate>().HasData(new TokenRate { Id = id, SkuId = id, BaseToken = r.Token, EffectiveFrom = epoch });
            foreach (var p in new[] { ("Retail", 1, (decimal?)r.Retail), ("Promo", 1, r.Promo), ("Wholesale", 1, r.Wholesale), ("Pack6", 6, r.Pack6), ("Pack12", 12, r.Pack12) })
                if (p.Item3 is decimal amount) b.Entity<ProductPrice>().HasData(new ProductPrice { Id = priceId++, SkuId = id, Tier = p.Item1, PackQuantity = p.Item2, Amount = amount, EffectiveFrom = epoch });
        }
        b.Entity<TokenPolicy>().HasData(new TokenPolicy { Id = 1, EffectiveFrom = epoch });
        foreach (var channel in Enum.GetValues<SalesChannel>())
            b.Entity<ChannelPolicy>().HasData(new ChannelPolicy { Id = (long)channel + 1, Channel = channel, AllowNetworkReward = channel is SalesChannel.Store or SalesChannel.POS or SalesChannel.Manual, AllowRedemption = channel is SalesChannel.Store or SalesChannel.POS });
        b.Entity<Warehouse>().HasData(new Warehouse { Id = 1, Name = "คลังกลาง AM HERB" });
        b.Entity<ShippingRule>().HasData(new ShippingRule { Id = 1, Fee = 50, FreeAbove = 1500 });
        var settings = new Dictionary<string, string> { ["RewardPendingDays"] = "14", ["MaxUplineRewardDepth"] = "3", ["TokenRedemptionReferenceTHB"] = "1.00", ["TokenTransferEnabled"] = "false", ["TokenCashWithdrawalEnabled"] = "false", ["TokenExpiryMonths"] = "12", ["MaxTokenRedemptionPercent"] = "100", ["ShippingRedeemableWithToken"] = "false", ["ReferralCookieDays"] = "30", ["AbandonedCheckoutHours"] = "24" };
        long settingId = 1;
        settings["TaxRatePercent"] = "0";
        foreach (var s in settings) b.Entity<SystemSetting>().HasData(new SystemSetting { Id = settingId++, Key = s.Key, Value = s.Value });
    }
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles) if (!await roles.RoleExistsAsync(role))
        { var result = await roles.CreateAsync(new IdentityRole(role)); if (!result.Succeeded) throw new InvalidOperationException("Role initialization failed."); }
        var email = Environment.GetEnvironmentVariable("AMHERB_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("AMHERB_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        var users = services.GetRequiredService<UserManager<AppUser>>();
        // Bootstrap only a new identity; never elevate an existing public registration.
        if (await users.FindByEmailAsync(email) != null) return;
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) throw new InvalidOperationException("Admin bootstrap failed: " + string.Join("; ", created.Errors.Select(x => x.Description)));
        var assigned = await users.AddToRoleAsync(user, "SuperAdmin");
        if (!assigned.Succeeded) throw new InvalidOperationException("Admin role assignment failed.");
        await services.GetRequiredService<Services.MemberService>().CreateAsync(user.Id, "AM HERB Administrator", null);
    }
}
