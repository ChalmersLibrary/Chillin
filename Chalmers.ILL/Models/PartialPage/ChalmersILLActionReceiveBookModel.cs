using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionReceiveBookModel : OrderItemPageModelBase
    {
        public string BookAvailableMailTemplate { get; set; }

        public ChalmersILLActionReceiveBookModel(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}