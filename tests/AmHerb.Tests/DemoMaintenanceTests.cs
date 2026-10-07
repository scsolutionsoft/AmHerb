using AmHerb.Web.Controllers;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace AmHerb.Tests;
public class DemoMaintenanceTests
{
    [Fact] public void Reset_endpoint_requires_SuperAdmin_and_POST()
    {
        Assert.Equal("SuperAdmin", Assert.Single(typeof(DemoController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        Assert.Single(typeof(DemoController).GetMethod(nameof(DemoController.Reset))!.GetCustomAttributes(typeof(HttpPostAttribute), true));
    }
    [SqlFact] public async Task Reset_preserves_baseline_admin_catalog_settings_snapshots_and_restores_SQL_protections()
    {
        var isolated = new SqlFixture(); await isolated.InitializeAsync();
        try
        {
            await using var h = new Harness(isolated); await h.InitializeAsync(stock: 0);
            var original = await h.MemberAsync(); var baselineProductCount = await h.Db.Products.CountAsync();
            var snapshot = new ReportSnapshot { Day = h.Clock.Now.Date, Sales = 42, Orders = 1, TokenLiability = 0 }; h.Db.ReportSnapshots.Add(snapshot);
            await h.Db.SaveChangesAsync();
            var environment = new DemoTestEnvironment(); var maintenance = new DemoDataMaintenance(h.Db, environment);
            await maintenance.CreateBaselineAsync();
            var originalProduct = await h.Db.Products.FirstAsync(); var originalName = originalProduct.Name;
            originalProduct.Name = "Edited during demo";
            await h.Inventory.ReceiveAsync(h.SkuId, h.WarehouseId, "RESET-TEST", h.Clock.Now.AddDays(-1), h.Clock.Now.AddYears(1), 100, 100, "Reset test");
            var added = await h.MemberAsync(original); var order = await h.SellAsync(added);
            Assert.NotEmpty(await h.Db.JournalEntries.ToListAsync());
            (await h.Db.SystemSettings.SingleAsync(x => x.Key == "TaxRatePercent")).Value = "7";
            snapshot.Sales = 999; snapshot.Orders = 9; await h.Db.SaveChangesAsync();
            await maintenance.ResetAsync(original.UserId);
            Assert.Equal(original.UserId, Assert.Single(await h.Db.Users.ToListAsync()).Id);
            Assert.Equal(original.Id, Assert.Single(await h.Db.Members.ToListAsync()).Id);
            Assert.Equal(baselineProductCount, await h.Db.Products.CountAsync());
            Assert.Equal(originalName, (await h.Db.Products.FindAsync(originalProduct.Id))!.Name);
            Assert.Empty(await h.Db.Orders.ToListAsync()); Assert.Empty(await h.Db.InventoryBatches.ToListAsync()); Assert.Empty(await h.Db.JournalEntries.ToListAsync());
            Assert.Equal("0", (await h.Db.SystemSettings.SingleAsync(x => x.Key == "TaxRatePercent")).Value);
            Assert.Equal(42m, (await h.Db.ReportSnapshots.SingleAsync()).Sales);
            Assert.False(await h.Db.SystemSettings.AnyAsync(x => x.Key.StartsWith("Demo.")));
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => h.Db.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs WHERE Action='Demo.Reset'"));
            var disabled = await h.Db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1").SingleAsync(); Assert.Equal(0, disabled);
            var triggers = await h.Db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE is_disabled=1 AND OBJECT_SCHEMA_NAME(parent_id)='dbo'").SingleAsync(); Assert.Equal(0, triggers);
            await Assert.ThrowsAsync<BusinessException>(() => maintenance.ResetAsync(original.UserId));
            await maintenance.CreateBaselineAsync(); // A clean reset can begin a new demo.
            await maintenance.ResetAsync(original.UserId);
            environment.EnvironmentName = "Production";
            await Assert.ThrowsAsync<BusinessException>(() => maintenance.CreateBaselineAsync());
        }
        finally { await isolated.DisposeAsync(); }
    }
    private sealed class DemoTestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "AmHerb.Web";
        public string ContentRootPath { get; set; } = "";
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
