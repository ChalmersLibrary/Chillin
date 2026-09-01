using System.Linq;
using System.Web.Mvc;
using Chalmers.ILL.Controllers.SurfaceControllers;
using Chalmers.ILL.Controllers.SurfaceControllers.Page;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.Controllers
{
    // Umbraco's "Public Access" node protection used to gate every page behind login; that
    // protection lived in the CMS content tree, not in code, so it silently vanished when
    // Umbraco was removed (only noticed once ChalmersILLController stopped crashing and the
    // unauthenticated homepage became visible). FilterConfig replaces it with a global
    // AuthorizeAttribute. These tests guard the wiring: the filter is registered, and exactly
    // the controllers that must stay public are marked [AllowAnonymous].
    [TestClass]
    public class AuthorizationTest
    {
        [TestMethod]
        public void RegisterGlobalFilters_AddsAuthorizeAttribute()
        {
            var filters = new GlobalFilterCollection();

            FilterConfig.RegisterGlobalFilters(filters);

            Assert.IsTrue(filters.Select(f => f.Instance).OfType<AuthorizeAttribute>().Any());
        }

        [TestMethod]
        public void LoginPageController_IsAllowAnonymous()
        {
            Assert.IsTrue(IsAllowAnonymous(typeof(ChalmersILLLoginPageController)));
        }

        [TestMethod]
        public void LoginSurfaceController_IsAllowAnonymous()
        {
            Assert.IsTrue(IsAllowAnonymous(typeof(LoginSurfaceController)));
        }

        [TestMethod]
        public void OrderItemReceivedAtBranchSurfaceController_IsAllowAnonymous()
        {
            // Physical delivery slips already printed with QR codes point at this URL and
            // can't be reprinted, so it must stay reachable without a login.
            Assert.IsTrue(IsAllowAnonymous(typeof(OrderItemReceivedAtBranchSurfaceController)));
        }

        [TestMethod]
        public void ChalmersILLController_RequiresLogin()
        {
            Assert.IsFalse(IsAllowAnonymous(typeof(ChalmersILLController)));
        }

        [TestMethod]
        public void ChalmersILLOrderListPageController_RequiresLogin()
        {
            Assert.IsFalse(IsAllowAnonymous(typeof(ChalmersILLOrderListPageController)));
        }

        [TestMethod]
        public void ChalmersILLSettingsPageController_RequiresLogin()
        {
            Assert.IsFalse(IsAllowAnonymous(typeof(ChalmersILLSettingsPageController)));
        }

        [TestMethod]
        public void ChalmersILLDiskPageController_RequiresLogin()
        {
            Assert.IsFalse(IsAllowAnonymous(typeof(ChalmersILLDiskPageController)));
        }

        private static bool IsAllowAnonymous(System.Type controllerType)
        {
            return controllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
        }
    }
}
