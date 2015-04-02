using Chalmers.ILL.Models;
using Examine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;

namespace Chalmers.ILL.Providers
{
    public class ProviderService : IProviderService
    {
        ISearcher _orderItemsSearcher;

        public ProviderService(ISearcher orderItemsSearcher)
        {
            _orderItemsSearcher = orderItemsSearcher;
        }

        public List<String> FetchAndCreateListOfUsedProviders()
        {
            var res = new List<String>();

            var searchCriteria = _orderItemsSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            // NOTE: Should probably only fetch orders that are not too old, to keep the numbers down and to keep the data relevant.
            var allOrders = _orderItemsSearcher.Search(searchCriteria.RawQuery("nodeTypeAlias:ChalmersILLOrderItem"));

            return allOrders.Where(x => x.Fields.ContainsKey("ProviderName") && x.Fields["ProviderName"] != "")
                .Select(x => x.Fields["ProviderName"])
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .ToList();
        }

        public int GetSuggestedDeliveryTimeInHoursForProvider(string providerName)
        {
            Int64 totalTime = 0;
            int count = 1;

            if (!String.IsNullOrWhiteSpace(providerName))
            {
                var searchCriteria = _orderItemsSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                // NOTE: Should probably only fetch orders that are not too old, to keep the numbers down and to keep the data relevant.
                var orders = _orderItemsSearcher.Search(searchCriteria.RawQuery("ProviderName:\"" + providerName + "\""));
                count = orders.Count() > 0 ? orders.Count() : 1;

                foreach (var order in orders)
                {
                    DateTime latestDeliveryStatus;
                    DateTime latestOrderedStatus;
                    foreach (var logItem in JsonConvert.DeserializeObject<List<LogItem>>(order.Fields.GetValueString("Log")))
                    {
                        latestDeliveryStatus = new DateTime(1970, 1, 1);
                        latestOrderedStatus = new DateTime(1970, 1, 1);

                        if (logItem.Type == "STATUS" && logItem.Message.Contains("till Beställd"))
                        {
                            latestOrderedStatus = logItem.CreateDate;
                        }
                        else if (logItem.Type == "STATUS" && (logItem.Message.Contains("till Levererad") || logItem.Message.Contains("till Utlånad")))
                        {
                            latestDeliveryStatus = logItem.CreateDate;
                        }

                        totalTime += (latestDeliveryStatus - latestOrderedStatus).Hours;
                    }
                }
            }

            return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(totalTime / count)));
        }
    }
}