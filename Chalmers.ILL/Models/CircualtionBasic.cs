using Newtonsoft.Json;

namespace Chalmers.ILL.Models
{
    public class CirculationBasic
    {
        public string ItemId { get; set; }
        public string RequesterId { get; set; }
        public string RequestType { get; set; }
        public string RequestDate { get; set; }
        public string FulFilmentPreference { get; set; }
        public string Status { get; set; }

        [JsonProperty("item")]
        public CircualtionBasicItem Item { get; set; }
        public string PickupServicePointId { get; set; }
    }

    public class CircualtionBasicItem
    {
        public string Barcode { get; set; }
    }
}