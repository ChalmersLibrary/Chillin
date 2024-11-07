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
            return SimpleSearch(query, 0, 10000).Items;
        }

        public SearchResult Search(string query, int start, int size)
        {
            return SimpleSearch(query, start, size);
        }

        public IEnumerable<OrderItemModel> Search(string query, int size, string[] fields)
        {
            return _elasticClient.Search<OrderItemModel>(s => s
                .From(0)
                .Size(size)
                .Source(sr => sr.Includes(fi => fi.Fields(fields)))
                .Type("orderitemmodel")
                .Query(q => q
                    .Bool(b =>
                        b.Must(m =>
                            m.QueryString(qs => qs
                                .Query(query)))))).Hits.Select(x => x.Source);
        }

        public IEnumerable<string> AggregatedProviders()
        {
            var res = new List<string>() { "TIB", "Libris", "Subito" };

            var response = _elasticClient.Search<OrderItemModel>(s => s
                .From(0)
                .Size(0)
                .Type("orderitemmodel")
                .Query(q => q
                    .Bool(b =>
                        b.Must(m =>
                            m.QueryString(qs => qs
                                .Query("NOT status:(Ny OR Annullerad OR Inköpt OR Överförd) AND NOT providerName:(\"libris\" OR \"subito\" OR \"tib\")")
                     ))))
                .Aggregations(a => a.Terms("providers", st => st
                    .Field("providerName.keyword")
                    .Size(10000))));

            if (response.IsValid)
            {
                var aggregatedProviders = response.Aggregations.Terms("providers").Buckets
                    .OrderByDescending(x => x.Count)
                    .Select(x => x.Key);
                res.AddRange(aggregatedProviders);
            }

            return res;
        }

        #region Private methods

        private SearchResult SimpleSearch(string query, int start, int size)
        {
            var esResponse = _elasticClient.Search<OrderItemModel>(s => s
                .From(start)
                .Size(size)
                .Type("orderitemmodel")
                .Query(q => q
                    .Bool(b =>
                        b.Must(m =>
                            m.QueryString(qs => qs
                                .Query(query))))));

            return new SearchResult
            {
                Count = esResponse.Total,
                Items = esResponse.Hits.Select(h => h.Source)
            };
        }

        #endregion
    }
}