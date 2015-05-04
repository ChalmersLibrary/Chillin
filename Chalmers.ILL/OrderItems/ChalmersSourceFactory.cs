﻿using Chalmers.ILL.Mail;
using Chalmers.ILL.Patron;
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
        INotifier _notifier;
        IMediaService _mediaService;
        IUmbracoWrapper _umbraco;
        IPatronDataProvider _patronDataProvider;

        public ChalmersSourceFactory(IExchangeMailWebApi exchangeMailWebApi, IOrderItemManager orderItemManager, INotifier notifier, 
            IMediaService mediaService, IUmbracoWrapper umbraco, IPatronDataProvider patronDataProvider)
        {
            _exchangeMailWebApi = exchangeMailWebApi;
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _mediaService = mediaService;
            _umbraco = umbraco;
            _patronDataProvider = patronDataProvider;
        }

        public List<ISource> Sources()
        {
            var res = new List<ISource>();
            res.Add(new ChalmersOrderItemsMailSource(_exchangeMailWebApi, _orderItemManager, _notifier, _mediaService, _patronDataProvider));
            res.Add(new LibrisOrderItemsSource(_umbraco, _orderItemManager, _patronDataProvider));
            return res;
        }
    }
}