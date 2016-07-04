using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.Page
{
    public class ChalmersILLOrderListPageModel : ChalmersILLModel
    {
        public IEnumerable<OrderItemModel> PendingOrderItems { get; set; }
    }
}