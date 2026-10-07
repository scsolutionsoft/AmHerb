using AmHerb.Web.Services;
using Microsoft.AspNetCore.Mvc;
namespace AmHerb.Web.Controllers;
public class AddressesController : Controller
{
    [HttpGet, ResponseCache(Duration = 86400)]
    public IActionResult Children(string? parent) => Json(ThaiAddresses.Children(parent));
}
