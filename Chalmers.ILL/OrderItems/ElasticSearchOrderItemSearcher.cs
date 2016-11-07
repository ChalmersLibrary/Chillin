using System.Collections.Generic;
using System.Linq;
using Chalmers.ILL.Models;
using Nest;
using System;

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
            _elasticClient.Index(item, x => x.Id(item.NodeId));
        }

        public void Deleted(OrderItemModel item)
        {
            _elasticClient.Delete<OrderItemModel>(item.NodeId);
        }

        public void Modified(OrderItemModel item)
        {
            _elasticClient.Index(item, x => x.Id(item.NodeId));
        }

        public IEnumerable<OrderItemModel> Search(string query)
        {
            return SimpleSearch(query);
        }

        public IEnumerable<string> AggregatedProviders()
        {
            var res = SimpleSearch("NOT status:(Ny OR Annullerad OR Inköpt OR Överförd) AND NOT providerName:(\"libris\" OR \"subito\" OR \"tib\")")
                .Where(x => !String.IsNullOrWhiteSpace(x.ProviderName)).Select(x => x.ProviderName).ToList();
            res.Add("Libris");
            res.Add("Subito");
            res.Add("TIB");
            return res.GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .ToList();
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