using System.Globalization;
using System.Text;
using AmHerb.Web.Domain;
using AmHerb.Web.Models;
using AmHerb.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmHerb.Web.Controllers;

[Authorize]
public class DashboardController(SalesDashboardService dashboards, Actor actor, IAuthorizationService authorization) : Controller
{
    public async Task<IActionResult> Index() => (await authorization.AuthorizeAsync(User, "Reports")).Succeeded
        ? RedirectToAction(nameof(Overview)) : RedirectToAction(nameof(Mine));

    [Authorize(Policy = "Reports")]
    public async Task<IActionResult> Overview(DateTime? from, DateTime? to, SalesChannel? channel)
        => await Report(null, from, to, channel, false);

    public async Task<IActionResult> Mine(DateTime? from, DateTime? to, SalesChannel? channel)
        => await Report(actor.Id, from, to, channel, false);

    [Authorize(Policy = "Reports")]
    public async Task<IActionResult> ExportOverview(DateTime? from, DateTime? to, SalesChannel? channel)
        => await Report(null, from, to, channel, true);

    public async Task<IActionResult> ExportMine(DateTime? from, DateTime? to, SalesChannel? channel)
        => await Report(actor.Id, from, to, channel, true);

    private async Task<IActionResult> Report(string? userId, DateTime? from, DateTime? to, SalesChannel? channel, bool export)
    {
        if (!ModelState.IsValid) return BusinessExceptionFilter.ErrorResult(HttpContext, "รูปแบบวันที่หรือช่องทางไม่ถูกต้อง กรุณาเลือกตัวกรองใหม่", 400);
        var model = await dashboards.BuildAsync(userId, from, to, channel);
        return export ? Csv(model) : View("Index", model);
    }

    public static string CsvCell(string value)
    {
        if ((value.TrimStart().Length > 0 && "=+-@".Contains(value.TrimStart()[0])) || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private FileContentResult Csv(SalesDashboard model)
    {
        static string N(decimal number) => number.ToString("0.00", CultureInfo.InvariantCulture);
        List<string[]> rows = [
            ["รายงาน", model.Overview ? "ภาพรวมบริษัท" : "ยอดขายของฉัน"],
            ["วันที่เริ่มต้น (กรุงเทพฯ)", model.Period.From.ToString("yyyy-MM-dd")],
            ["วันที่สิ้นสุด (กรุงเทพฯ)", model.Period.To.ToString("yyyy-MM-dd")],
            ["ช่องทาง", model.Channel == null ? "ทุกช่องทาง" : SalesDashboard.ChannelLabel(model.Channel.Value)],
            ["ยอดขายสุทธิสินค้า", N(model.Current.Net)], ["คำสั่งซื้อขาย", model.Current.Orders.ToString()],
            ["จำนวนชิ้นสุทธิ", model.Current.Units.ToString()], ["ยอดเฉลี่ยต่อคำสั่งซื้อ", N(model.Current.Average)] ];
        if (model.Overview) { rows.Add(["ต้นทุนสินค้าสุทธิ", N(model.Current.Cost)]); rows.Add(["กำไรหลังต้นทุนและค่าธรรมเนียม", N(model.Current.Contribution)]); }
        rows.Add(["วันที่", "ยอดขายสุทธิ", "ช่วงก่อนหน้า (วันเทียบเคียง)"]);
        rows.AddRange(model.Days.Select(x => new[] { x.Day.ToString("yyyy-MM-dd"), N(x.Net), N(x.Previous) }));
        rows.Add(["SKU", "สินค้า", "จำนวนชิ้นสุทธิ", "ยอดขายสุทธิ (10 อันดับแรก)"]);
        rows.AddRange(model.Products.Select(x => new[] { x.Code, x.Name, x.Units.ToString(), N(x.Net) }));
        rows.Add(["แคมเปญ", "คำสั่งซื้อ", "จำนวนชิ้นสุทธิ", "ยอดขายสุทธิ (10 อันดับแรก)"]);
        rows.AddRange(model.Campaigns.Select(x => new[] { x.Label, x.Orders.ToString(), x.Units.ToString(), N(x.Net) }));
        var text = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(CsvCell))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray(), "text/csv; charset=utf-8", $"AMHERB-{(model.Overview ? "overview" : "my-sales")}-{model.Period.From:yyyyMMdd}-{model.Period.To:yyyyMMdd}.csv");
    }
}
