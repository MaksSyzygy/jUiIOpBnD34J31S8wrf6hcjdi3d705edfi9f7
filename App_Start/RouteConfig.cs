using System.Web.Mvc;
using System.Web.Routing;

namespace Checkitlink
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("Link", "Link/{action}/{name}", new { controller = "Link", action = "Index", name = UrlParameter.Optional }, new[] { "Checkitlink.Controllers" });

            routes.MapRoute("Home", "Home/{action}/{name}", new { controller = "Home", action = "Index", name = UrlParameter.Optional }, new[] { "Checkitlink.Controllers" });

            routes.MapRoute("User", "User/{action}/{name}", new { controller = "User", action = "Index", name = UrlParameter.Optional }, new[] { "Checkitlink.Controllers" });

            routes.MapRoute("Default", "", new { controller = "Home", action = "Index" }, new[] { "BankApplication.Controllers" });
        }
    }
}
