using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
    }
}