using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionProviderModel : OrderItemPageModelBase
    {
        public List<String> Providers { get; set; }
        public DateTime EstimatedDeliveryCurrentProvider { get; set; }

        public ChalmersILLActionProviderModel(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}