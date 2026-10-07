using AmHerb.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace AmHerb.Tests;

[Collection("SQL")]
public class MemberStoreMigrationTests(SqlFixture fixture)
{
    [SqlFact] public async Task Delivery_upgrade_preserves_existing_demo_snapshot_shapes()
    {
        var database = "AmHerb_Test_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(fixture.Connection) { InitialCatalog = database };
        await using var db = new AmHerbDbContext(new DbContextOptionsBuilder<AmHerbDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261007071642_MemberStores");
            await db.Database.ExecuteSqlRawAsync("SELECT TOP(0) * INTO dbo.AmHerbDemoBaseline_Orders FROM dbo.Orders; SELECT TOP(0) * INTO dbo.AmHerbDemoBaseline_Payments FROM dbo.Payments; SELECT TOP(0) * INTO dbo.AmHerbDemoBaseline_Shipments FROM dbo.Shipments; INSERT INTO dbo.SystemSettings ([Key],[Value]) VALUES (N'Demo.State',N'Ready');");
            await migrator.MigrateAsync();
            Assert.Equal(3, await db.SystemSettings.CountAsync(x => x.Key == "Demo.Baseline.PaymentSlips" || x.Key == "Demo.Baseline.StoreShippingOptions" || x.Key == "Demo.Baseline.ShippingProviders"));
            await db.Database.ExecuteSqlRawAsync("SELECT HouseNumber,SelectedCarrier,SlipRequired FROM dbo.AmHerbDemoBaseline_Orders; SELECT SubmittedReference,SubmittedAt FROM dbo.AmHerbDemoBaseline_Payments; SELECT Data FROM dbo.AmHerbDemoBaseline_PaymentSlips;");
            Assert.Equal(4, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.AmHerbDemoBaseline_ShippingProviders").SingleAsync());
            await migrator.MigrateAsync("20261007160726_OrderSlipsAndDelivery");
            await migrator.MigrateAsync();
            Assert.Equal(4, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.AmHerbDemoBaseline_ShippingProviders").SingleAsync());
        }
        finally
        {
            if (connection.InitialCatalog != database || !database.StartsWith("AmHerb_Test_", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe test database cleanup.");
            await db.Database.EnsureDeletedAsync();
        }
    }
    [SqlFact] public async Task Upgrade_and_rollback_keep_existing_demo_snapshot_compatible()
    {
        var database = "AmHerb_Test_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(fixture.Connection) { InitialCatalog = database };
        await using var db = new AmHerbDbContext(new DbContextOptionsBuilder<AmHerbDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            var migrations = db.Database.GetMigrations().ToList(); var current = migrations.FindIndex(x => x.EndsWith("_MemberStores")); Assert.True(current > 0);
            var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(migrations[current - 1]);
            await db.Database.ExecuteSqlRawAsync("SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_Orders FROM dbo.Orders; SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_Carts FROM dbo.Carts; INSERT INTO dbo.SystemSettings ([Key],[Value]) VALUES (N'Demo.State',N'Ready');");
            await migrator.MigrateAsync(migrations[current]);
            Assert.Equal(3, await db.SystemSettings.CountAsync(x => x.Key == "Demo.Baseline.MemberStores" || x.Key == "Demo.Baseline.StoreProducts" || x.Key == "Demo.Baseline.StoreExpenses"));
            // Reading the new columns proves baseline restore will have compatible columns.
            await db.Database.ExecuteSqlRawAsync("SELECT StoreId,StoreName FROM dbo.AmHerbDemoBaseline_Orders; SELECT StoreId FROM dbo.AmHerbDemoBaseline_Carts; SELECT * FROM dbo.AmHerbDemoBaseline_StoreExpenses;");
            await migrator.MigrateAsync(migrations[current - 1]);
            Assert.False(await db.SystemSettings.AnyAsync(x => x.Key == "Demo.Baseline.MemberStores"));
            await migrator.MigrateAsync(migrations[current]);
            Assert.Empty(await db.MemberStores.ToListAsync());
        }
        finally
        {
            if (connection.InitialCatalog != database || !database.StartsWith("AmHerb_Test_", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe test database cleanup.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
