using Chalmers.ILL.Configuration;
using Chalmers.ILL.Mail;
using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Members;
using Chalmers.ILL.Models;
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
using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;
using Unity.Mvc4;

namespace Chalmers.ILL
{
    public class FakeFolio : IFolioItemService, IFolioRepository, IFolioService, IFolioInstanceService,
        IFolioHoldingService, IFolioCirculationService, IFolioUserService
    {
        public ItemQuery ByQuery(string query)
        {
            return new ItemQuery
            {
                Items = new Item[0],
                TotalRecords = 0
            };
        }

        public FolioUser ByUserName(string userName)
        {
            return new FolioUser
            {
                Id = "7312031232",
                Personal = new Personal
                {
                    FirstName = "John",
                    LastName = "Doe"
                }
            };
        }

        public void InitFolio(string title, string orderId, string barcode, string pickUpServicePoint, bool readOnlyAtLibrary, string patronCardNumber)
        {
            Console.WriteLine("INIT FOLIO");
        }

        public Item Post(ItemBasic item, bool readOnlyAtLibrary)
        {
            Console.WriteLine("POST FOLIO ITEM");

            return null;
        }

        public string Post(string path, string body)
        {
            Console.WriteLine("POST FOLIO PATH BODY");

            return "tjosan";
        }

        public Instance Post(InstanceBasic item)
        {
            Console.WriteLine("POST FOLIO INSTANCE");

            return null;
        }

        public Holding Post(HoldingBasic item)
        {
            Console.WriteLine("POST FOLIO HOLDING BASIC");

            return null;
        }

        public Circulation Post(CirculationBasic item)
        {
            Console.WriteLine("POST FOLIO CIRCULATION");

            return null;
        }

        public void Put(Item item)
        {
            Console.WriteLine("PUT FOLIO ITEM");
        }

        public string Put(string path, string body)
        {
            Console.WriteLine("PUT FOLIO PATH BODY");

            return "tjosan";
        }

        public void SetItemToWithdrawn(string id)
        {
            Console.WriteLine("FOLIO SET ITEM TO WITHDRAWN " + id);
        }

        string IFolioRepository.ByQuery(string path)
        {
            return "IFolioRepository";
        }
    }

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
            container.RegisterInstance(new HttpClient());

            if (config.UseMicrosoftGraphMailService)
            {
                container.RegisterType<IExchangeMailWebApi, MicrosoftGraphMailWebApi>();
            }
            else
            {
                container.RegisterType<IExchangeMailWebApi, ExchangeMailWebApi>();
            }
            container.RegisterType<ISourceFactory, ChalmersSourceFactory>();
            container.RegisterType<IMediaItemManager, BlobStorageMediaItemManager>();
            container.RegisterType<IMediaItemManager, UmbracoMediaItemManager>("Legacy");
            container.RegisterType<IOrderItemSearcher, ElasticSearchOrderItemSearcher>();
            container.RegisterType<ITemplateService, ElasticsearchTemplateService>();
            container.RegisterType<IAffiliationDataProvider, PdbAffiliationDataProvider>();

            container.RegisterType<IChillinTextRepository, ChillinTextRepository>();
            container.RegisterType<IJsonService, JsonService>();

            // Uncomment to not touch FOLIO
            /*container.RegisterType<IFolioItemService, FakeFolio>();
            container.RegisterType<IFolioRepository, FakeFolio>();
            container.RegisterType<IFolioService, FakeFolio>();
            container.RegisterType<IFolioInstanceService, FakeFolio>();
            container.RegisterType<IFolioHoldingService, FakeFolio>();
            container.RegisterType<IFolioCirculationService, FakeFolio>();
            container.RegisterType<IFolioUserService, FakeFolio>();*/

            // Comment these to not touch FOLIO
            container.RegisterType<IFolioItemService, FolioItemService>();
            container.RegisterType<IFolioRepository, FolioRepository>();
            container.RegisterType<IFolioService, FolioService>();
            container.RegisterType<IFolioInstanceService, FolioInstanceService>();
            container.RegisterType<IFolioHoldingService, FolioHoldingService>();
            container.RegisterType<IFolioCirculationService, FolioCirculationService>();
            container.RegisterType<IFolioUserService, FolioUserService>();

            container.RegisterType<IPersonDataProvider, PdbPersonDataProvider>();

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
            container.RegisterInstance<IPatronDataProvider>(new FolioPatronDataProvider(templateService, affiliationDataProvider));
        }
    }
}