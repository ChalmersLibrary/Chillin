using Chalmers.ILL.Models;
using System.Collections.Generic;

namespace Chalmers.ILL.OrderItems
{
    public interface IOrderItemSearcher
    {
        IEnumerable<OrderItemModel> Search(string query);
        SearchResult Search(string query, int start, int size);
        IEnumerable<OrderItemModel> Search(string query, int size, string[] fields);
        IEnumerable<string> AggregatedProviders();

        void Added(OrderItemModel item);
        void Modified(OrderItemModel item);
        void Deleted(OrderItemModel item);
    }
}
