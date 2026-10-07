using AmHerb.Web.Domain;
namespace AmHerb.Web.Controllers;
public record ShippingLabelPage(Order Order, string SenderName, string SenderPhone, string SenderAddress);
