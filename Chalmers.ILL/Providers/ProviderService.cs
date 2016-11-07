﻿using Chalmers.ILL.Models;
using Examine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Providers
{
    public class ProviderService : IProviderService
    {
        IOrderItemSearcher _orderItemsSearcher;

        public ProviderService(IOrderItemSearcher orderItemsSearcher)
        {
            _orderItemsSearcher = orderItemsSearcher;
        }

        public IEnumerable<String> FetchAndCreateListOfUsedProviders()
        {
            return _orderItemsSearcher.AggregatedProviders();
        }

        public int GetSuggestedDeliveryTimeInHoursForProvider(string providerName)
        {
            int res = 168;

            if (!String.IsNullOrWhiteSpace(providerName) && providerName.ToLower().Contains("subito"))
            {
                res = 24;
            }

            return res;
        }
    }
}