using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLOrderItemModel : OrderItemPageModelBase
    {
        public ChalmersILLOrderItemModel(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}