using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class SearchResult
    {
        public long Count { get; set; }
        public IEnumerable<OrderItemModel> Items { get; set; }
    }
}