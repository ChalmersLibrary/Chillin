namespace Chalmers.ILL.Models
{
    public class CirculationBasic
    {
        public string ItemId { get; set; }
        public string RequesterId { get; set; }
        public string RequestType { get; set; }
        public string RequestDate { get; set; }
        public string FulfilmentPreference { get; set; }
        public string PickupServicePointId { get; set; }
    }
}