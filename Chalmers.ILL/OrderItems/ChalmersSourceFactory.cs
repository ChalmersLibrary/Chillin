using Chalmers.ILL.Mail;
using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Patron;
using Chalmers.ILL.Providers;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.UmbracoApi;
using System.Collections.Generic;

namespace Chalmers.ILL.OrderItems
{
    public class ChalmersSourceFactory : ISourceFactory
    {
        IExchangeMailWebApi _exchangeMailWebApi;
        IOrderItemManager _orderItemManager;
        INotifier _notifier;
        IMediaItemManager _mediaItemManager;
        IUmbracoWrapper _umbraco;
        IPatronDataProvider _patronDataProvider;
        IPersonDataProvider _personDataProvider;
        IOrderItemSearcher _orderItemSearcher;

        public ChalmersSourceFactory(IExchangeMailWebApi exchangeMailWebApi, IOrderItemManager orderItemManager, INotifier notifier, 
            IMediaItemManager mediaItemManager, IUmbracoWrapper umbraco, IPatronDataProvider patronDataProvider, IPersonDataProvider personDataProvider, 
            IOrderItemSearcher orderItemSearcher)
        {
            _exchangeMailWebApi = exchangeMailWebApi;
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _mediaItemManager = mediaItemManager;
            _umbraco = umbraco;
            _patronDataProvider = patronDataProvider;
            _personDataProvider = personDataProvider;
            _orderItemSearcher = orderItemSearcher;
        }

        public List<ISource> Sources()
        {
            var res = new List<ISource>();
            res.Add(new ChalmersOrderItemsMailSource(_exchangeMailWebApi, _orderItemManager, _notifier, _mediaItemManager, _patronDataProvider, _personDataProvider, _orderItemSearcher));
            // res.Add(new LibrisOrderItemsSource(_umbraco, _orderItemManager, _patronDataProvider, _orderItemSearcher)); Removed due to service being discontinued 2025-09-08
            return res;
        }
    }
}