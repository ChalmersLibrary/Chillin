using Chalmers.ILL.DependencyResolution;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Chalmers.ILL.Tests.DependencyResolution
{
    [TestClass]
    public class UnityDependencyResolversTest
    {
        interface IThing { }
        class ThingA : IThing { }
        class ThingB : IThing { }

        class DisposableThing : System.IDisposable
        {
            public bool WasDisposed { get; private set; }
            public void Dispose() { WasDisposed = true; }
        }

        [TestMethod]
        public void MvcResolver_GetService_ReturnsRegisteredInstance()
        {
            var container = new UnityContainer();
            container.RegisterType<IThing, ThingA>();
            var resolver = new UnityMvcDependencyResolver(container);

            var result = resolver.GetService(typeof(IThing));

            Assert.IsInstanceOfType(result, typeof(ThingA));
        }

        [TestMethod]
        public void MvcResolver_GetService_ReturnsNullWhenUnregistered()
        {
            var container = new UnityContainer();
            var resolver = new UnityMvcDependencyResolver(container);

            var result = resolver.GetService(typeof(IThing));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void MvcResolver_GetServices_ReturnsAllRegisteredInstances()
        {
            var container = new UnityContainer();
            container.RegisterType<IThing, ThingA>("a");
            container.RegisterType<IThing, ThingB>("b");
            var resolver = new UnityMvcDependencyResolver(container);

            var result = resolver.GetServices(typeof(IThing)).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(x => x is ThingA));
            Assert.IsTrue(result.Any(x => x is ThingB));
        }

        [TestMethod]
        public void MvcResolver_GetServices_ReturnsEmptyWhenUnregistered()
        {
            var container = new UnityContainer();
            var resolver = new UnityMvcDependencyResolver(container);

            var result = resolver.GetServices(typeof(IThing));

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void WebApiResolver_GetService_ReturnsRegisteredInstance()
        {
            var container = new UnityContainer();
            container.RegisterType<IThing, ThingA>();
            var resolver = new UnityWebApiDependencyResolver(container);

            var result = resolver.GetService(typeof(IThing));

            Assert.IsInstanceOfType(result, typeof(ThingA));
        }

        [TestMethod]
        public void WebApiResolver_GetService_ReturnsNullWhenUnregistered()
        {
            var container = new UnityContainer();
            var resolver = new UnityWebApiDependencyResolver(container);

            var result = resolver.GetService(typeof(IThing));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void WebApiResolver_BeginScope_ResolvesFromChildContainer()
        {
            var container = new UnityContainer();
            container.RegisterType<IThing, ThingA>();
            var resolver = new UnityWebApiDependencyResolver(container);

            using (var scope = resolver.BeginScope())
            {
                var result = scope.GetService(typeof(IThing));

                Assert.IsInstanceOfType(result, typeof(ThingA));
            }
        }

        [TestMethod]
        public void WebApiResolver_Dispose_DisposesUnderlyingContainer()
        {
            var container = new UnityContainer();
            var registeredInstance = new DisposableThing();
            container.RegisterInstance(registeredInstance);
            var resolver = new UnityWebApiDependencyResolver(container);

            resolver.Dispose();

            Assert.IsTrue(registeredInstance.WasDisposed);
        }
    }
}
