using System.Linq;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.Controllers
{
    // Umbraco's own view engine registration used to add "~/Views/Partials/{0}.cshtml" as a
    // search location. All order-action partials (Chalmers.ILL.Action.*, DeliveryType/*,
    // Settings/*, Chalmers.ILL.OrderItem, Chalmers.ILL.LogItem — ~20 files) live under
    // Views/Partials/ and are referenced by bare name via PartialView("..."), relying on that
    // location. It silently disappeared when Umbraco's view engine registration was removed,
    // so every PartialView(...) call targeting one of those files failed at runtime with "the
    // partial view '...' was not found" — invisible to controller-level unit tests, since they
    // call the action method directly without going through the view engine at all.
    [TestClass]
    public class ViewEngineConfigTest
    {
        [TestMethod]
        public void RegisterViewEngines_AddsViewsPartialsToPartialViewLocationFormats()
        {
            var engines = new ViewEngineCollection { new RazorViewEngine() };

            ViewEngineConfig.RegisterViewEngines(engines);

            var razorEngine = (RazorViewEngine)engines.Single();
            Assert.IsTrue(razorEngine.PartialViewLocationFormats.Contains("~/Views/Partials/{0}.cshtml"));
        }

        [TestMethod]
        public void RegisterViewEngines_AddsViewsPartialsToViewLocationFormats()
        {
            var engines = new ViewEngineCollection { new RazorViewEngine() };

            ViewEngineConfig.RegisterViewEngines(engines);

            var razorEngine = (RazorViewEngine)engines.Single();
            Assert.IsTrue(razorEngine.ViewLocationFormats.Contains("~/Views/Partials/{0}.cshtml"));
        }

        [TestMethod]
        public void RegisterViewEngines_PreservesExistingLocationFormats()
        {
            var engines = new ViewEngineCollection { new RazorViewEngine() };
            var originalCount = ((RazorViewEngine)engines.Single()).PartialViewLocationFormats.Length;

            ViewEngineConfig.RegisterViewEngines(engines);

            var razorEngine = (RazorViewEngine)engines.Single();
            Assert.AreEqual(originalCount + 1, razorEngine.PartialViewLocationFormats.Length);
            Assert.IsTrue(razorEngine.PartialViewLocationFormats.Contains("~/Views/Shared/{0}.cshtml"));
        }
    }
}
