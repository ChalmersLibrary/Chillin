using System.Linq;
using System.Web.Mvc;

namespace Chalmers.ILL
{
    public class ViewEngineConfig
    {
        // Umbraco's own view engine registration used to add "~/Views/Partials/{0}.cshtml" as a
        // search location (Umbraco itself follows that convention for macro partials). All ~20
        // order-action partials (Chalmers.ILL.Action.*, DeliveryType/*, Settings/*,
        // Chalmers.ILL.OrderItem, Chalmers.ILL.LogItem) live under Views/Partials/ and are
        // referenced by bare name via PartialView("..."), relying on that search location. It
        // silently disappeared when Umbraco's view engine registration was removed, so every one
        // of those PartialView(...) calls fails at runtime with "the partial view '...' was not
        // found" — not caught by unit tests, since they call controller actions directly without
        // going through the view engine.
        public static void RegisterViewEngines(ViewEngineCollection engines)
        {
            var razorEngine = engines.OfType<RazorViewEngine>().FirstOrDefault();
            if (razorEngine == null) return;

            razorEngine.PartialViewLocationFormats = razorEngine.PartialViewLocationFormats
                .Concat(new[] { "~/Views/Partials/{0}.cshtml" })
                .ToArray();

            razorEngine.ViewLocationFormats = razorEngine.ViewLocationFormats
                .Concat(new[] { "~/Views/Partials/{0}.cshtml" })
                .ToArray();
        }
    }
}
