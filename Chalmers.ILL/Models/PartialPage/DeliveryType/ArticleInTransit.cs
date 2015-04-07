using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage.DeliveryType
{
    public class ArticleInTransit : OrderItemPageModelBase
    {
        public bool DrmWarning { get; set; }
        public string ArticleDeliveryLibrary { get; set; }

        public ArticleInTransit(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}