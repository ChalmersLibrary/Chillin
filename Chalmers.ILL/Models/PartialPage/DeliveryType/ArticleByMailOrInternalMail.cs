using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage.DeliveryType
{
    public class ArticleByMailOrInternalMail : OrderItemPageModelBase
    {
        public bool DrmWarning { get; set; }

        public ArticleByMailOrInternalMail(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}