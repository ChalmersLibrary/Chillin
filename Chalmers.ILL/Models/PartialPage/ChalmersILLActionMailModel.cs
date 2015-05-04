using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionMailModel : OrderItemPageModelBase
    {
        public string SignatureTemplate { get; set; }

        public ChalmersILLActionMailModel(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}