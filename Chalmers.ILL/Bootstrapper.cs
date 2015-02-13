using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Unity.Mvc4;
using Chalmers.ILL.Members;
using System.Web.Http;
using Umbraco.Web;

namespace Chalmers.ILL
{
    public static class Bootstrapper
    {
        public static IUnityContainer Initialise()
        {
            var container = BuildUnityContainer();
            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            return container;
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();

            RegisterTypes(container);

            return container;
        }

        public static void RegisterTypes(IUnityContainer container)
        {
            container.RegisterType<IMemberInfoManager, MemberInfoManager>();

            container.RegisterInstance(typeof(UmbracoContext), UmbracoContext.Current);
        }
    }
}