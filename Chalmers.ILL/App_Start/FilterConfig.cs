using System.Web.Mvc;

namespace Chalmers.ILL
{
    public class FilterConfig
    {
        // Umbraco's "Public Access" node protection used to gate every page behind login;
        // that protection lived in the CMS content tree, not in code, so it silently vanished
        // when Umbraco was removed. This filter replaces it: everything requires a logged-in
        // session unless the controller/action opts out with [AllowAnonymous] (the login page,
        // the login POST handler, and the QR-code branch-receipt endpoint that must stay public).
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new AuthorizeAttribute());
        }
    }
}
