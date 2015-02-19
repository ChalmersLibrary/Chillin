using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionLogEntryModel : OrderItemPageModelBase
    {
        public ChalmersILLActionLogEntryModel(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}