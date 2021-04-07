using Chalmers.ILL.Configuration;
using Chalmers.ILL.Mail;
using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Members;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Patron;
using Chalmers.ILL.Providers;
using Chalmers.ILL.Repositories;
using Chalmers.ILL.Services;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using Microsoft.Practices.Unity;
using Nest;
using System.Web.Http;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;
using Unity.Mvc4;

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
            elasticClientSettings.DefaultIndex(config.ElasticSearchIndex);
            var elasticClient = new ElasticClient(elasticClientSettings);

            container.RegisterInstance(typeof(IContentService), ApplicationContext.Current.Services.ContentService);
            container.RegisterInstance(typeof(IMediaService), ApplicationContext.Current.Services.MediaService);
            container.RegisterInstance<IElasticClient>(elasticClient);

            container.RegisterType<IExchangeMailWebApi, ExchangeMailWebApi>();
            container.RegisterType<ISourceFactory, ChalmersSourceFactory>();
            container.RegisterType<IMediaItemManager, BlobStorageMediaItemManager>();
            container.RegisterType<IMediaItemManager, UmbracoMediaItemManager>("Legacy");
            container.RegisterType<IOrderItemSearcher, ElasticSearchOrderItemSearcher>();
            container.RegisterType<ITemplateService, ElasticsearchTemplateService>();
            container.RegisterType<IAffiliationDataProvider, SolrLibcdksAffiliationDataProvider>();

            container.RegisterType<IJsonService, JsonService>();
            container.RegisterType<IFolioItemService, FolioItemService>();
            container.RegisterType<IFolioRepository, FolioRepository>();
            container.RegisterType<IFolioService, FolioService>();
            container.RegisterType<IFolioInstanceService, FolioInstanceService>();
            container.RegisterType<IFolioHoldingService, FolioHoldingService>();
            container.RegisterType<IFolioCirculationService, FolioCirculationService>();
            container.RegisterType<IChillinTextRepository, ChillinTextRepository>();
            container.RegisterType<IFolioInventoryItemService, FolioInventoryItemService>();

            var templateService = container.Resolve<ITemplateService>();
            var affiliationDataProvider = container.Resolve<IAffiliationDataProvider>();

            // Create all our singleton type instances.
            var mailService = new MailService(container.Resolve<IMediaItemManager>(), container.Resolve<IExchangeMailWebApi>());
            var notifier = new Notifier();
            var umbraco = new UmbracoWrapper();
            var orderItemManager = new EntityFrameworkOrderItemManager(umbraco, container.Resolve<IOrderItemSearcher>());
            var legacyOrderItemManager = new OrderItemManager(umbraco);
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
            container.RegisterInstance<IOrderItemManager>("Legacy", legacyOrderItemManager);
            container.RegisterInstance(typeof(IAutomaticMailSendingEngine), new AutomaticMailSendingEngine(container.Resolve<IOrderItemSearcher>(), templateService, orderItemManager, mailService));
            container.RegisterInstance(typeof(IMailService), mailService);
            container.RegisterInstance(typeof(IProviderService), providerService);
            container.RegisterInstance(typeof(IBulkDataManager), bulkDataManager);
            container.RegisterInstance<IPatronDataProvider>(new FolioPatronDataProvider(templateService, affiliationDataProvider).Connect());
        }
    }
}