using Chalmers.ILL.Models;
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

        public List<String> FetchAndCreateListOfUsedProviders()
        {
            var res = new List<String>();

            // NOTE: Should probably only fetch orders that are not too old, to keep the numbers down and to keep the data relevant.
            var allOrders = _orderItemsSearcher.Search("nodeTypeAlias:ChalmersILLOrderItem");

            return allOrders.Where(x => x.ProviderName != "")
                .Select(x => x.ProviderName)
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .ToList();
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