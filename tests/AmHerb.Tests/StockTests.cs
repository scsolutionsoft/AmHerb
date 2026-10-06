using System.Security.Claims;
using System.Text;
using AmHerb.Web.Controllers;
using AmHerb.Web.Domain;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Tests;
[Collection("SQL")]
public class StockTests(SqlFixture fixture)
{
    private static ClaimsPrincipal Principal(Member member, string role = "Member") => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, member.UserId), new Claim(ClaimTypes.Role, role)], "Test"));
    private static StockController Controller(Harness h, ClaimsPrincipal user)
    {
        var context = new DefaultHttpContext { User = user };
        return new(h.Db, new AuditService(h.Db, new Actor(new HttpContextAccessor { HttpContext = context })), h.Clock) { ControllerContext = new ControllerContext { HttpContext = context } };
    }
    [SqlFact] public async Task Stock_report_scopes_owner_filters_export_and_preserves_physical_totals()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var owner = await h.MemberAsync(); var outsider = await h.MemberAsync();
        var warehouse = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id);
        await h.Inventory.ReceiveAsync(h.SkuId, warehouse.Id, "OWNER-LOT", h.Clock.Now.AddDays(-20), h.Clock.Now.AddDays(20), 12, 137, "Owner stock");
        var batch = await h.Db.InventoryBatches.SingleAsync(x => x.WarehouseId == warehouse.Id); batch.QtyAvailable = 9; batch.QtyReserved = 3; await h.Db.SaveChangesAsync();
        var controller = Controller(h, Principal(owner));
        var result = Assert.IsType<ViewResult>(await controller.Index(null, null, null, null)); var model = Assert.IsType<StockPage>(result.Model);
        var row = Assert.Single(model.Rows); Assert.Equal(12, row.OnHand); Assert.Equal(9, row.Available); Assert.Equal(3, row.Reserved); Assert.Equal(9, row.Expiring); Assert.False(model.Staff);
        var csv = Assert.IsType<FileContentResult>(await controller.Export(null, null, null, null)); var text = Encoding.UTF8.GetString(csv.FileContents); Assert.Contains("OWNER-LOT", (await h.Db.InventoryBatches.FindAsync(batch.Id))!.LotNo); Assert.DoesNotContain("มูลค่าทุน", text);
        var other = Controller(h, Principal(outsider)); var forbidden = Assert.IsType<ViewResult>(await other.Index(null, warehouse.Id, null, null)); Assert.Empty(Assert.IsType<StockPage>(forbidden.Model).Rows);
        var otherCsv = Assert.IsType<FileContentResult>(await other.Export(null, warehouse.Id, null, null)); Assert.DoesNotContain(warehouse.Name, Encoding.UTF8.GetString(otherCsv.FileContents));
        h.Clock.Now = h.Clock.Now.AddDays(21);
        var expired = Assert.IsType<StockPage>(Assert.IsType<ViewResult>(await controller.Index(null, null, null, "expired")).Model); Assert.Equal(9, Assert.Single(expired.Rows).Expired); Assert.Equal(0, expired.Rows[0].Available); Assert.Equal(12, expired.Rows[0].OnHand);
        var finance = Controller(h, Principal(owner, "Finance")); var global = Assert.IsType<StockPage>(Assert.IsType<ViewResult>(await finance.Index(WarehouseKind.Company, h.WarehouseId, null, null)).Model); Assert.Equal(100, Assert.Single(global.Rows).OnHand); Assert.True(global.Staff);
    }
    [SqlFact] public async Task Registration_creates_private_warehouse_and_POS_without_central_stock_access()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var user = new AppUser { UserName = Guid.NewGuid().ToString("N") }; h.Db.Users.Add(user); await h.Db.SaveChangesAsync();
        var member = await h.Members.CreateAsync(user.Id, "Private owner", null);
        var register = await h.Db.PosRegisters.Include(x => x.Warehouse).SingleAsync(x => x.UserId == user.Id);
        Assert.Equal(member.Id, register.Warehouse.OwnerMemberId); Assert.Equal(WarehouseKind.Member, register.Warehouse.Kind); Assert.NotEqual(1, register.WarehouseId);
        var visible = await StockController.Visible(h.Db, Principal(member)).ToListAsync(); Assert.Equal(register.WarehouseId, Assert.Single(visible).Id);
    }
    [SqlFact] public async Task Stock_alerts_are_calculated_per_warehouse_and_delivered_to_owner_once()
    {
        await using var h = new Harness(fixture); await h.InitializeAsync();
        var owner = await h.MemberAsync(); var warehouse = await h.Db.Warehouses.SingleAsync(x => x.OwnerMemberId == owner.Id);
        await h.Inventory.ReceiveAsync(h.SkuId, warehouse.Id, "ALERT", h.Clock.Now.AddDays(-10), h.Clock.Now.AddDays(20), 2, 100, "Low personal stock");
        var batch = await h.Db.InventoryBatches.SingleAsync(x => x.WarehouseId == warehouse.Id);
        var jobs = new RecurringJobs(h.Db, h.Rewards, h.Commerce, new NotificationService(h.Db), [], h.Clock);
        await jobs.RunAsync(CancellationToken.None); await jobs.RunAsync(CancellationToken.None);
        var lowKey = $"low:{warehouse.Id}:{h.SkuId}:{h.Clock.Now:yyyyMMdd}";
        var low = Assert.Single(await h.Db.Notifications.Where(x => x.EventKey == lowKey).ToListAsync()); Assert.Equal(owner.UserId, low.UserId);
        var expiry = Assert.Single(await h.Db.Notifications.Where(x => x.EventKey == $"expiry:{batch.Id}:30").ToListAsync()); Assert.Equal(owner.UserId, expiry.UserId);
        Assert.False(await h.Db.Notifications.AnyAsync(x => x.EventKey == $"low:{h.WarehouseId}:{h.SkuId}:{h.Clock.Now:yyyyMMdd}"));
    }
}
