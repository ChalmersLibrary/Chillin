using System;

namespace Chalmers.ILL.Models
{
    public class Request
    {
        public string Id { get; set; }
        public string RequestType { get; set; }
        public string RequestDate { get; set; }
        public string RequesterId { get; set; }
        public string ItemId { get; set; }
        public string FulfilmentPreference { get; set; }
        public Metadata Metadata { get; set; }
    }
}