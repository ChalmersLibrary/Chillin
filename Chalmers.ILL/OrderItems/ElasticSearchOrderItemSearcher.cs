using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Models;

namespace Chalmers.ILL.OrderItems
{
    public class ElasticSearchOrderItemSearcher : IOrderItemSearcher
    {
        public void Added(OrderItemModel item)
        {
            throw new NotImplementedException();
        }

        public void Deleted(OrderItemModel item)
        {
            throw new NotImplementedException();
        }

        public void Modified(OrderItemModel item)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<OrderItemModel> Search(string query)
        {
            throw new NotImplementedException();
        }
    }
}