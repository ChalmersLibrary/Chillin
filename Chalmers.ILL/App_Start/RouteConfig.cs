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

            // Backwards-compatibility aliases for the old Umbraco content-tree slugs that pointed
            // at the order list and settings pages before the Umbraco removal. The app's own links
            // use these slugs again (see ChalmersILL.cshtml et al.), so these routes are what make
            // them resolve; the wildcard segment absorbs a trailing slash or stray path/query noise.
            // Settings alias must be registered before the order-list alias, since the order-list
            // alias's wildcard would otherwise swallow "/bestaellningar/instaellningar" too.
            routes.MapRoute(
                name: "LegacyBestaellningarInstaellningarSlugAlias",
                url: "bestaellningar/instaellningar/{*pathInfo}",
                defaults: new { controller = "ChalmersILLSettingsPage", action = "Index", pathInfo = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "LegacyBestaellningarSlugAlias",
                url: "bestaellningar/{*pathInfo}",
                defaults: new { controller = "ChalmersILLOrderListPage", action = "Index", pathInfo = UrlParameter.Optional }
            );

            // Backwards-compatibility alias for the old Umbraco content-tree slug for the Desk
            // landing page. LoginSurfaceController redirects members with the "Desk" role here
            // after login; this route is what makes that URL resolve.
            routes.MapRoute(
                name: "LegacyDiskSlugAlias",
                url: "disk/{*pathInfo}",
                defaults: new { controller = "ChalmersILLDiskPage", action = "Index", pathInfo = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "ChalmersILL", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
