using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AmHerb.Tests;

public class SqlFactAttribute : FactAttribute
{ public SqlFactAttribute() { if (Environment.GetEnvironmentVariable("AMHERB_RUN_SQL_TESTS") != "1") Skip = "Set AMHERB_RUN_SQL_TESTS=1 to create and remove an isolated SQL Server test database."; } }
[CollectionDefinition("SQL", DisableParallelization = true)] public class SqlCollection : ICollectionFixture<SqlFixture> { }
public class SqlFixture : IAsyncLifetime
{
    public string Connection { get; private set; } = "";
    private readonly string databaseName = "AmHerb_Test_" + Guid.NewGuid().ToString("N");
    public AmHerbDbContext Db() => new(new DbContextOptionsBuilder<AmHerbDbContext>().UseSqlServer(Connection).Options);
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("AMHERB_RUN_SQL_TESTS") != "1") return;
        var cfg = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
        var connection = new SqlConnectionStringBuilder(cfg.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing test SQL Server connection.")) { InitialCatalog = databaseName };
        Connection = connection.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(Connection)) return;
        var parsed = new SqlConnectionStringBuilder(Connection);
        if (parsed.InitialCatalog != databaseName || !databaseName.StartsWith("AmHerb_Test_", StringComparison.Ordinal) || !Guid.TryParseExact(databaseName[12..], "N", out _)) throw new InvalidOperationException("Unsafe test database cleanup rejected.");
        await using var db = Db(); await db.Database.EnsureDeletedAsync();
    }
}
public class TestClock : TimeProvider
{
    public DateTime Now { get; set; } = DateTime.UtcNow;
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
}
public sealed class Harness : IAsyncDisposable
{
    public AmHerbDbContext Db { get; }
    public TestClock Clock { get; } = new();
    public MemberService Members { get; }
    public RewardService Rewards { get; }
    public PricingService Prices { get; }
    public InventoryService Inventory { get; }
    public CommerceService Commerce { get; }
    public PosService Pos { get; }
    public long WarehouseId { get; private set; }
    public long SkuId { get; private set; }
    public Harness(SqlFixture fixture)
    {
        Db = fixture.Db(); var actor = new Actor(new HttpContextAccessor()); var audit = new AuditService(Db, actor); var notifications = new NotificationService(Db);
        Members = new(Db, audit, notifications); Prices = new(Db, Clock); Rewards = new(Db, Prices, Clock, notifications);
        Inventory = new(Db, Clock, audit); Commerce = new(Db, Prices, Rewards, Inventory, audit, Clock, notifications); Pos = new(Db, Members, audit, Clock);
    }
    public async Task InitializeAsync(int stock = 100, decimal price = 990, decimal tokens = 90)
    {
        var warehouse = new Warehouse { Name = "TEST " + Guid.NewGuid().ToString("N") }; Db.Warehouses.Add(warehouse);
        var sku = new Sku { Product = new Product { Name = "TEST " + Guid.NewGuid().ToString("N") }, Code = Guid.NewGuid().ToString("N") };
        Db.Skus.Add(sku); await Db.SaveChangesAsync(); WarehouseId = warehouse.Id; SkuId = sku.Id;
        Db.ProductPrices.Add(new ProductPrice { SkuId = SkuId, Amount = price, EffectiveFrom = Clock.Now.AddDays(-1) });
        Db.TokenRates.Add(new TokenRate { SkuId = SkuId, BaseToken = tokens, EffectiveFrom = Clock.Now.AddDays(-1) }); await Db.SaveChangesAsync();
        if (stock > 0) await Inventory.ReceiveAsync(SkuId, WarehouseId, "TEST", Clock.Now.AddDays(-30), Clock.Now.AddYears(2), stock, 100, "Test fixture");
    }
    public async Task<Member> MemberAsync(Member? sponsor = null)
    {
        var email = Guid.NewGuid().ToString("N") + "@example.invalid";
        var user = new AppUser { Id = Guid.NewGuid().ToString(), Email = email, NormalizedEmail = email.ToUpperInvariant(), UserName = email, NormalizedUserName = email.ToUpperInvariant(), EmailConfirmed = true };
        Db.Users.Add(user); await Db.SaveChangesAsync();
        var m = await Members.CreateAsync(user.Id, "Test member", sponsor?.ReferralCode);
        (await Db.PosRegisters.SingleAsync(x => x.UserId == user.Id)).WarehouseId = WarehouseId; await Db.SaveChangesAsync(); return m;
    }
    public async Task<PosSession> OpenAsync(Member seller, decimal cash = 0)
    { await Pos.OpenAsync(seller.UserId, cash); return await Db.PosSessions.SingleAsync(x => x.PosRegister.UserId == seller.UserId && x.ClosedAt == null); }
    public async Task<Order> SellAsync(Member seller, int quantity = 1, Member? buyer = null, string? key = null, bool loading = false)
    {
        var session = await Db.PosSessions.SingleOrDefaultAsync(x => x.PosRegister.UserId == seller.UserId && x.ClosedAt == null) ?? await OpenAsync(seller);
        return await Commerce.CheckoutAsync(new CheckoutInput { Key = key ?? Guid.NewGuid().ToString("N"), CustomerName = "External customer", Phone = "0900000000", StockLoading = loading }, [new(SkuId, quantity)], buyer?.Id, seller.Id, SalesChannel.POS, seller.UserId, session.Id, 100000);
    }
    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
