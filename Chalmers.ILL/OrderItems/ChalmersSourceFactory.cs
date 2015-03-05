using Chalmers.ILL.Logging;
using Chalmers.ILL.Mail;
using Chalmers.ILL.Providers;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.UmbracoApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Services;

namespace Chalmers.ILL.OrderItems
{
    public class ChalmersSourceFactory : ISourceFactory
    {
        IExchangeMailWebApi _exchangeMailWebApi;
        IOrderItemManager _orderItemManager;
        IInternalDbLogger _internalDbLogger;
        INotifier _notifier;
        IMediaService _mediaService;
        IUmbracoWrapper _umbraco;

        public ChalmersSourceFactory(IExchangeMailWebApi exchangeMailWebApi, IOrderItemManager orderItemManager,
            IInternalDbLogger internalDbLogger, INotifier notifier, IMediaService mediaService,
            IUmbracoWrapper umbraco)
        {
            _exchangeMailWebApi = exchangeMailWebApi;
            _orderItemManager = orderItemManager;
            _internalDbLogger = internalDbLogger;
            _notifier = notifier;
            _mediaService = mediaService;
            _umbraco = umbraco;
        }

        public List<ISource> Sources()
        {
            var res = new List<ISource>();
            res.Add(new ChalmersOrderItemsMailSource(_exchangeMailWebApi, _orderItemManager, _internalDbLogger, _notifier, _mediaService));
            res.Add(new LibrisOrderItemsSource(_umbraco, _orderItemManager, _internalDbLogger));
            return res;
        }
    }
}