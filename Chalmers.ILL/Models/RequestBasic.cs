namespace Chalmers.ILL.Models
{
    public class RequestBasic
    {
        public string ItemId { get; set; }
        public string RequesterId { get; set; }
        public string RequestType { get; set; }
        public string RequestDate { get; set; }
        public string FulFilmentPreference { get; set; }
    }
}