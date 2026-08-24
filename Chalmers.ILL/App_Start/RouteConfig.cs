using System.Web.Mvc;
using System.Web.Routing;

namespace Chalmers.ILL
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Backwards-compatibility alias for the old Umbraco surface-controller URL scheme
            // (/umbraco/surface/{Controller}/{Action}). Controller/action names are unchanged
            // by the Umbraco removal, so this maps straight onto the same routes as "Default".
            // Kept indefinitely: physical delivery slips already printed with QR codes pointing
            // at this URL (see OrderItemReceivedAtBranchSurfaceController) can't be reprinted.
            routes.MapRoute(
                name: "LegacyUmbracoSurfaceAlias",
                url: "umbraco/surface/{controller}/{action}/{id}",
                defaults: new { id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "ChalmersILL", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
