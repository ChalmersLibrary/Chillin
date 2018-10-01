using System.Collections.Generic;
using System.Linq;
using Chalmers.ILL.Models;
using Nest;
using System;
using Newtonsoft.Json;

namespace Chalmers.ILL.OrderItems
{
    public class ElasticSearchOrderItemSearcher : IOrderItemSearcher
    {
        private IElasticClient _elasticClient;

        public ElasticSearchOrderItemSearcher(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public void Added(OrderItemModel item)
        {
            var indexResponse = _elasticClient.Index(item, x => x.Id(item.NodeId));

            if (indexResponse.ApiCall == null || !indexResponse.ApiCall.Success)
            {
                throw new OrderItemSearchIndexingException("Failed to index added order item. Reason: " + indexResponse.DebugInformation + "; Data:" + JsonConvert.SerializeObject(item));
            }                    
        }

        public void Deleted(OrderItemModel item)
        {
            var deleteResponse = _elasticClient.Delete<OrderItemModel>(item.NodeId);

            if (deleteResponse.ApiCall == null || !deleteResponse.ApiCall.Success)
            {
                throw new OrderItemSearchIndexingException("Failed to delete order item. Reason: " + deleteResponse.DebugInformation + "; Data:" + JsonConvert.SerializeObject(item));
            }
        }

        public void Modified(OrderItemModel item)
        {
            var indexResponse = _elasticClient.Index(item, x => x.Id(item.NodeId));

            if (indexResponse.ApiCall == null || !indexResponse.ApiCall.Success)
            {
                throw new OrderItemSearchIndexingException("Failed to index modified order item. Reason: " + indexResponse.DebugInformation + "; Data:" + JsonConvert.SerializeObject(item));
            }
        }

        public IEnumerable<OrderItemModel> Search(string query)
        {
            return SimpleSearch(query);
        }

        public IEnumerable<string> AggregatedProviders()
        {
            // REMARK: This solution is indeed very ugly. The problem is that the Elastic Search analyzer splits the terms up on whitespaces.
            // Which means that we get unique words instead of unique providers. To solve this the right way the index should be changed so that
            // the providerName field is a multi value field. In this way we should be able to access the raw data for the field and still be able
            // to get proper aggregations down to word level and be able to search on provider names.
            var res = new List<string>() { "TIB", "Libris", "Subito" };
            var aggregatedProviders = SimpleSearch("NOT status:(Ny OR Annullerad OR Inköpt OR Överförd) AND NOT providerName:(\"libris\" OR \"subito\" OR \"tib\")")
                .Where(x => !String.IsNullOrWhiteSpace(x.ProviderName)).Select(x => x.ProviderName)
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .ToList();
            res.AddRange(aggregatedProviders);
            return res;
        }

        #region Private methods

        private IEnumerable<OrderItemModel> SimpleSearch(string query)
        {
            return _elasticClient.Search<OrderItemModel>(s => s
                .From(0)
                .Size(10000)
                .AllTypes()
                .Query(q => q
                    .Bool(b =>
                        b.Must(m =>
                            m.QueryString(qs =>
                                qs.DefaultField("_all")
                                .Query(query)))))).Hits.Select(x => x.Source);
        }

        #endregion
    }
}