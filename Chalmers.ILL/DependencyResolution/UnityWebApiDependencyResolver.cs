using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

namespace Chalmers.ILL.DependencyResolution
{
    public class UnityWebApiDependencyResolver : IDependencyResolver
    {
        private readonly IUnityContainer _container;

        public UnityWebApiDependencyResolver(IUnityContainer container)
        {
            _container = container;
        }

        public IDependencyScope BeginScope()
        {
            return new UnityWebApiDependencyResolver(_container.CreateChildContainer());
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _container.Resolve(serviceType);
            }
            catch (ResolutionFailedException)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return _container.ResolveAll(serviceType);
            }
            catch (ResolutionFailedException)
            {
                return new object[0];
            }
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
