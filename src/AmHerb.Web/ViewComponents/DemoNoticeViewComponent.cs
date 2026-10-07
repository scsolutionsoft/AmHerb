using AmHerb.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.ViewComponents;
public class DemoNoticeViewComponent(AmHerbDbContext db, IWebHostEnvironment environment) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!environment.IsDevelopment()) return Content("");
        var state = await db.SystemSettings.AsNoTracking().Where(x => x.Key == "Demo.State").Select(x => x.Value).SingleOrDefaultAsync();
        return state == null ? Content("") : View("Default", state);
    }
}
