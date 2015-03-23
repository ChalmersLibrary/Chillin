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
using Chalmers.ILL.Templates;
using Examine;
using Chalmers.ILL.Patron;
using System.Configuration;

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
            container.RegisterType<IExchangeMailWebApi, ExchangeMailWebApi>();
            container.RegisterType<ISourceFactory, ChalmersSourceFactory>();

            // Fetch all needed Examine search providers.
            var templatesSearcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLTemplatesSearcher"];
            var orderItemsSearcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

            // Create all our singleton type instances.
            var mailService = new MailService(ApplicationContext.Current.Services.MediaService, container.Resolve<IExchangeMailWebApi>());
            var templateService = new TemplateService(ApplicationContext.Current.Services.ContentService, templatesSearcher);
            var notifier = new Notifier();
            var internalDbLogger = new InternalDbLogger();
            var orderItemManager = new OrderItemManager();

            // Connect instances that depend on eachother.
            notifier.SetOrderItemManager(orderItemManager);
            internalDbLogger.SetOrderItemManager(orderItemManager);
            orderItemManager.SetNotifier(notifier);
            orderItemManager.SetInternalDbLogger(internalDbLogger);

            // Hook up everything that is needed for us to function.
            container.RegisterInstance(typeof(UmbracoContext), UmbracoContext.Current);
            container.RegisterInstance(typeof(IMemberInfoManager), new MemberInfoManager());
            container.RegisterInstance(typeof(IUmbracoWrapper), new UmbracoWrapper());
            container.RegisterInstance(typeof(INotifier), notifier);
            container.RegisterInstance(typeof(IInternalDbLogger), internalDbLogger);
            container.RegisterInstance(typeof(IOrderItemManager), orderItemManager);
            container.RegisterInstance(typeof(IContentService), ApplicationContext.Current.Services.ContentService);
            container.RegisterInstance(typeof(IMediaService), ApplicationContext.Current.Services.MediaService);
            container.RegisterInstance(typeof(ITemplateService), templateService);
            container.RegisterInstance(typeof(IAutomaticMailSendingEngine), new AutomaticMailSendingEngine(orderItemsSearcher));
            container.RegisterInstance(typeof(IPatronDataProvider), new Sierra(ConfigurationManager.AppSettings["sierraConnectionString"]).Connect());
            container.RegisterInstance(typeof(IMailService), mailService);
            container.RegisterInstance<ISearcher>("TemplatesSearcher", templatesSearcher);
            container.RegisterInstance<ISearcher>("OrderItemsSearcher", orderItemsSearcher);
        }
    }
}