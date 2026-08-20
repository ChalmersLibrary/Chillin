using System;
using System.IO;
using System.Web;
using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.Controllers
{
    [TestClass]
    public class RoutingTest
    {
        [TestMethod]
        public void DefaultRoute_ControllerAction_MapsCorrectly()
        {
            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);

            var routeData = GetRouteData(routes, "http://localhost/ChalmersILL/Index");

            Assert.IsNotNull(routeData);
            Assert.AreEqual("ChalmersILL", routeData.Values["controller"]);
            Assert.AreEqual("Index", routeData.Values["action"]);
        }

        [TestMethod]
        public void DefaultRoute_RootUrl_MapsToChalmersILLController()
        {
            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);

            var routeData = GetRouteData(routes, "http://localhost/");

            Assert.IsNotNull(routeData);
            Assert.AreEqual("ChalmersILL", routeData.Values["controller"]);
            Assert.AreEqual("Index", routeData.Values["action"]);
        }

        [TestMethod]
        public void DefaultRoute_OrderListPage_MapsToOrderListController()
        {
            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);

            var routeData = GetRouteData(routes, "http://localhost/ChalmersILLOrderListPage/Index");

            Assert.IsNotNull(routeData);
            Assert.AreEqual("ChalmersILLOrderListPage", routeData.Values["controller"]);
        }

        [TestMethod]
        public void DefaultRoute_AxdResource_UsesStopRoutingHandler()
        {
            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);

            var routeData = GetRouteData(routes, "http://localhost/WebResource.axd/foo");

            Assert.IsNotNull(routeData);
            Assert.IsInstanceOfType(routeData.RouteHandler, typeof(System.Web.Routing.StopRoutingHandler));
        }

        private static RouteData GetRouteData(RouteCollection routes, string url)
        {
            var uri = new Uri(url);
            var appRelativePath = "~" + (uri.AbsolutePath == "/" ? "/" : uri.AbsolutePath);
            return routes.GetRouteData(new StubHttpContext(appRelativePath));
        }

        private class StubHttpContext : HttpContextBase
        {
            private readonly StubHttpRequest _request;
            private readonly HttpServerUtilityBase _server = new StubServerUtility();

            public StubHttpContext(string appRelativePath)
            {
                _request = new StubHttpRequest(appRelativePath);
            }

            public override HttpRequestBase Request => _request;
            public override HttpServerUtilityBase Server => _server;
            public override bool IsDebuggingEnabled => false;
        }

        private class StubHttpRequest : HttpRequestBase
        {
            private readonly string _appRelativePath;

            public StubHttpRequest(string appRelativePath)
            {
                _appRelativePath = appRelativePath;
            }

            public override string AppRelativeCurrentExecutionFilePath => _appRelativePath;
            public override string PathInfo => string.Empty;
        }

        private class StubServerUtility : HttpServerUtilityBase
        {
            public override string MapPath(string path) => @"C:\nonexistent";
        }
    }
}
