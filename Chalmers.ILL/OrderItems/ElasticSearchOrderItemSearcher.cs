using System.Collections.Generic;
using System.Linq;
using Chalmers.ILL.Models;
using Nest;

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
            return _elasticClient.Search<OrderItemModel>(s => s
                .AllTypes()
                .Query(q => q
                    .Bool(b => 
                        b.Must(m => 
                            m.QueryString(qs => 
                                qs.DefaultField("_all")
                                .Query(query)))))).Hits.Select(x => x.Source);
        }
    }
}