using Newtonsoft.Json;

namespace Chalmers.ILL.Models
{
    public class Circulation
    {
        public string Id { get; set; }
        public string RequestType { get; set; }
        public string RequestDate { get; set; }
        public string RequesterId { get; set; }
        public string ItemId { get; set; }
        public string Status { get; set; }

        [JsonProperty("item")]
        public CirculationItem Item { get; set; }
        public Requester Requester { get; set; }
        public string FulfilmentPreference { get; set; }
        public string PickupServicePointId { get; set; }
        public Metadata Metadata { get; set; }
        public int Position { get; set; }
        public Pickupservicepoint PickupServicePoint { get; set; }
    }

    public class CirculationItem
    {
        public string Title { get; set; }
        public string Barcode { get; set; }
        public object[] Identifiers { get; set; }
        public string HoldingsRecordId { get; set; }
        public string InstanceId { get; set; }
        public Location Location { get; set; }
        public object[] ContributorNames { get; set; }
        public string Status { get; set; }
    }

    public class Location
    {
        public string Name { get; set; }
        public string LibraryName { get; set; }
        public string Code { get; set; }
    }

    public class Requester
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Barcode { get; set; }
        public string PatronGroupId { get; set; }
    }

    public class Pickupservicepoint
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string DiscoveryDisplayName { get; set; }
        public object Description { get; set; }
        public object ShelvingLagTime { get; set; }
        public bool PickupLocation { get; set; }
    }
}