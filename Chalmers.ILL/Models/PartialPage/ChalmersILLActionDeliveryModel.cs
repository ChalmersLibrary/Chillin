using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionDeliveryModel : OrderItemPageModelBase
    {
        public ChalmersILLActionDeliveryModel(OrderItemModel orderItemModel) : base(orderItemModel) { }

        public string ArticleDeliveryByMailTemplate { get; set; }
        public string BookAvailableMailTemplate { get; set; }
        public string BookSlipTemplate { get; set; }
    }
}