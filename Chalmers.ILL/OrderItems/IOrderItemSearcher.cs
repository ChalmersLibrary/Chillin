using Chalmers.ILL.Models;
using System.Collections.Generic;

namespace Chalmers.ILL.OrderItems
{
    public interface IOrderItemSearcher
    {
        IEnumerable<OrderItemModel> Search(string query);
    }
}
