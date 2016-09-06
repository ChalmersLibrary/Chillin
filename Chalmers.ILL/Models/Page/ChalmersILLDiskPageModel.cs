using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.Page
{
    public class ChalmersILLDiskPageModel : ChalmersILLModel
    {
        public ChalmersILLDiskPageModel()
        {
            OrderItems = new List<OrderItemModel>();
        }

        public IEnumerable<OrderItemModel> OrderItems { get; set; }
    }
}