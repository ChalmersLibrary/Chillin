﻿using Chalmers.ILL.Models;
using System.Collections.Generic;

namespace Chalmers.ILL.OrderItems
{
    public interface IOrderItemSearcher
    {
        IEnumerable<OrderItemModel> Search(string query);
        IEnumerable<string> AggregatedProviders();

        void Added(OrderItemModel item);
        void Modified(OrderItemModel item);
        void Deleted(OrderItemModel item);
    }
}
