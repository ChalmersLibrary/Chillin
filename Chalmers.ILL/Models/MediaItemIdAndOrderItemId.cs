using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class MediaItemIdAndOrderItemId
    {
        public MediaItemIdAndOrderItemId(int mediaItemId, int orderItemId)
        {
            MediaItemId = mediaItemId;
            OrderItemId = orderItemId;
        }

        public int MediaItemId { get; }
        public int OrderItemId { get; }
    }
}