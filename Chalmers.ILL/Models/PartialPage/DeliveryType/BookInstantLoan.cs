using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage.DeliveryType
{
    public class BookInstantLoan : OrderItemPageModelBase
    {
        public string BookAvailableMailTemplate { get; set; }
        public string BookSlipTemplate { get; set; }

        public BookInstantLoan(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}