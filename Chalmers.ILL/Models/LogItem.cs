using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class LogItem
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public int OrderItemNodeId { get; set; }
        public int NodeId { get; set; }

        public string EventId { get; set; }
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
