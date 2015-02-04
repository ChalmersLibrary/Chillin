using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class OutgoingMailModel
    {
        public int nodeId { get; set; } 
        public string recipientEmail { get; set; } 
        public string recipientName { get; set; } 
        public string message { get; set; }
        public string newStatus { get; set; }
        public string newFollowUpDate { get; set; }
        public List<int> attachments { get; set; }
    }
}