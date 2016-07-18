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
using Chalmers.ILL.MediaItems;
using Nest;
using Chalmers.ILL.Configuration;

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
            container.RegisterType<IConfiguration, DefaultChillinConfiguration>();
            var config = container.Resolve<IConfiguration>();

            var elasticClientSettings = new ConnectionSettings(new System.Uri(config.ElasticSearchUrl));
            elasticClientSettings.DefaultIndex("chillin");
            var elasticClient = new ElasticClient(elasticClientSettings);

            container.RegisterInstance(typeof(IContentService), ApplicationContext.Current.Services.ContentService);
            container.RegisterInstance(typeof(IMediaService), ApplicationContext.Current.Services.MediaService);
            container.RegisterInstance<IElasticClient>(elasticClient);

            container.RegisterType<IExchangeMailWebApi, ExchangeMailWebApi>();
            container.RegisterType<ISourceFactory, ChalmersSourceFactory>();
            container.RegisterType<IMediaItemManager, BlobStorageMediaItemManager>();
            container.RegisterType<IOrderItemSearcher, ElasticSearchOrderItemSearcher>();

            // Fetch all needed Examine search providers.
            var templatesSearcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLTemplatesSearcher"];

            // Create all our singleton type instances.
            var mailService = new MailService(container.Resolve<IMediaItemManager>(), container.Resolve<IExchangeMailWebApi>());
            var templateService = new TemplateService(ApplicationContext.Current.Services.ContentService, templatesSearcher);
            var notifier = new Notifier();
            var umbraco = new UmbracoWrapper();
            var orderItemManager = new EntityFrameworkOrderItemManager(umbraco, container.Resolve<IOrderItemSearcher>());
            var providerService = new ProviderService(container.Resolve<IOrderItemSearcher>());
            var bulkDataManager = new BulkDataManager(container.Resolve<IOrderItemSearcher>());

            // Connect instances that depend on eachother.
            notifier.SetOrderItemManager(orderItemManager, umbraco);
            orderItemManager.SetNotifier(notifier);

            // Hook up everything that is needed for us to function.
            container.RegisterInstance(typeof(UmbracoContext), UmbracoContext.Current);
            container.RegisterInstance(typeof(IMemberInfoManager), new MemberInfoManager());
            container.RegisterInstance(typeof(IUmbracoWrapper), umbraco);
            container.RegisterInstance(typeof(INotifier), notifier);
            container.RegisterInstance(typeof(IOrderItemManager), orderItemManager);
            container.RegisterInstance(typeof(ITemplateService), templateService);
            container.RegisterInstance(typeof(IAutomaticMailSendingEngine), new AutomaticMailSendingEngine(container.Resolve<IOrderItemSearcher>(), templateService, orderItemManager, mailService));
            container.RegisterInstance(typeof(IMailService), mailService);
            container.RegisterInstance(typeof(IProviderService), providerService);
            container.RegisterInstance(typeof(IBulkDataManager), bulkDataManager);
            container.RegisterInstance<ISearcher>("TemplatesSearcher", templatesSearcher);
            container.RegisterInstance<IPatronDataProvider>(new SierraCache(umbraco, templateService).Connect());
            container.RegisterInstance<IPatronDataProvider>("Sierra", new Sierra(umbraco, templateService, ConfigurationManager.AppSettings["sierraConnectionString"]).Connect());
        }
    }
}