using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Unity.Mvc4;
using System.Web.Http;
using Umbraco.Web;
using Chalmers.ILL.Members;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.Logging;
using Chalmers.ILL.Mail;
using Chalmers.ILL.UmbracoApi;
using Umbraco.Core;
using Umbraco.Core.Services;

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
            var notifier = new Notifier();
            var internalDbLogger = new InternalDbLogger();
            var orderItemManager = new OrderItemManager();
            notifier.SetOrderItemManager(orderItemManager);
            internalDbLogger.SetOrderItemManager(orderItemManager);
            orderItemManager.SetNotifier(notifier);
            orderItemManager.SetInternalDbLogger(internalDbLogger);

            container.RegisterInstance(typeof(UmbracoContext), UmbracoContext.Current);
            container.RegisterInstance(typeof(IMemberInfoManager), new MemberInfoManager());
            container.RegisterInstance(typeof(IUmbracoWrapper), new UmbracoWrapper());
            container.RegisterInstance(typeof(INotifier), notifier);
            container.RegisterInstance(typeof(IInternalDbLogger), internalDbLogger);
            container.RegisterInstance(typeof(IOrderItemManager), orderItemManager);
            container.RegisterInstance(typeof(IContentService), ApplicationContext.Current.Services.ContentService);
            container.RegisterInstance(typeof(IMediaService), ApplicationContext.Current.Services.MediaService);
            container.RegisterType<IExchangeMailWebApi, ExchangeMailWebApi>();
            container.RegisterType<ISourceFactory, ChalmersSourceFactory>();
        }
    }
}