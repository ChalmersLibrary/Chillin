using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.OrderItems
{
    public class SourcePollingResult
    {
        public SourcePollingResult()
        {
            Messages = new List<string>();
        }

        public string SourceName { get; set; }
        public int NewOrders { get; set; }
        public int UpdatedOrders { get; set; }
        public int Errors { get; set; }
        public List<string> Messages { get; set; }
    }
}