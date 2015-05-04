using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionClaimModel : OrderItemPageModelBase
    {
        public string ClaimBookMailTemplate { get; set; }
        public DateTime ClaimDueDate { get; set; }

        public ChalmersILLActionClaimModel(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}