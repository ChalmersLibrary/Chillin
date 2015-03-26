using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage.DeliveryType
{
    public class ArticleInInfodisk : OrderItemPageModelBase
    {
        public bool DrmWarning { get; set; }

        public ArticleInInfodisk(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}