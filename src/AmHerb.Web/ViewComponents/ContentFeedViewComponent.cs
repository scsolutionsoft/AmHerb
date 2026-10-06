using AmHerb.Web.Data;
using AmHerb.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AmHerb.Web.ViewComponents;
public class ContentFeedViewComponent(AmHerbDbContext db,TimeProvider clock):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()=>View(await NewsController.Published(db,User.Identity?.IsAuthenticated==true,clock.GetUtcNow().UtcDateTime).OrderByDescending(x=>x.Kind).ThenByDescending(x=>x.StartsAt).Take(6).ToListAsync());
}
