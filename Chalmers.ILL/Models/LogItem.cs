using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class LogItem
    {
        [Required]
        public int OrderItemNodeId { get; set; }
        public int NodeId { get; set; }

        public string Type { get; set; }
        public string Message { get; set; }
        public string MemberName { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public class LogItems
    {
        public List<LogItem> LogItemsList { get; set; }
    }
}
