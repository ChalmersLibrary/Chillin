using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Unity.Mvc4;
using System.Web.Http;
using Umbraco.Web;
using Chalmers.ILL.Members;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.Mail;
using Chalmers.ILL.UmbracoApi;
using Umbraco.Core;
using Umbraco.Core.Services;
using Chalmers.ILL.Templates;
using Examine;
using Chalmers.ILL.Patron;
using System.Configuration;
using Chalmers.ILL.Providers;

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
            var umbraco = new UmbracoWrapper();
            var orderItemManager = new OrderItemManager(umbraco);
            var providerService = new ProviderService(orderItemsSearcher);
            var bulkDataManager = new BulkDataManager(orderItemsSearcher);

            // Connect instances that depend on eachother.
            notifier.SetOrderItemManager(orderItemManager, umbraco);
            orderItemManager.SetNotifier(notifier);
            orderItemManager.SetContentService(ApplicationContext.Current.Services.ContentService);

            // Hook up everything that is needed for us to function.
            container.RegisterInstance(typeof(UmbracoContext), UmbracoContext.Current);
            container.RegisterInstance(typeof(IMemberInfoManager), new MemberInfoManager());
            container.RegisterInstance(typeof(IUmbracoWrapper), umbraco);
            container.RegisterInstance(typeof(INotifier), notifier);
            container.RegisterInstance(typeof(IOrderItemManager), orderItemManager);
            container.RegisterInstance(typeof(IContentService), ApplicationContext.Current.Services.ContentService);
            container.RegisterInstance(typeof(IMediaService), ApplicationContext.Current.Services.MediaService);
            container.RegisterInstance(typeof(ITemplateService), templateService);
            container.RegisterInstance(typeof(IAutomaticMailSendingEngine), new AutomaticMailSendingEngine(orderItemsSearcher, templateService, orderItemManager, mailService));
            container.RegisterInstance(typeof(IPatronDataProvider), new SierraCache(umbraco, templateService).Connect());
            container.RegisterInstance(typeof(IMailService), mailService);
            container.RegisterInstance(typeof(IProviderService), providerService);
            container.RegisterInstance(typeof(IBulkDataManager), bulkDataManager);
            container.RegisterInstance<ISearcher>("TemplatesSearcher", templatesSearcher);
            container.RegisterInstance<ISearcher>("OrderItemsSearcher", orderItemsSearcher);
        }
    }
}