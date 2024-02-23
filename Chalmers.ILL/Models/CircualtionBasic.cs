using System;

namespace Chalmers.ILL.Models
{
    public class CirculationBasic
    {
        public string ItemId { get; set; }
        public string RequesterId { get; set; }
        public string RequestType { get; set; } = "Page";
        public string RequestLevel { get; set; } = "Item";
        public string RequestDate { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        public string FulfillmentPreference { get; set; } = "Hold Shelf";
        public string PickupServicePointId { get; set; }
        public string InstanceId { get; set; }
        public string HoldingsRecordId { get; set; }

       
        public CirculationBasic(string itemId, string requesterId, string pickupServicePoint, string instanceId, string holdingId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException(nameof(itemId));
            }
            if (string.IsNullOrEmpty(requesterId))
            {
                throw new ArgumentNullException(nameof(requesterId));
            }
            if (string.IsNullOrEmpty(pickupServicePoint))
            {
                throw new ArgumentNullException(nameof(pickupServicePoint));
            }
            if (string.IsNullOrEmpty(instanceId))
            {
                throw new ArgumentNullException(nameof(instanceId));
            }
            if (string.IsNullOrEmpty(holdingId))
            {
                throw new ArgumentNullException(nameof(holdingId));
            }
            ItemId = itemId;
            RequesterId = requesterId;
            PickupServicePointId = pickupServicePoint;
            InstanceId = instanceId;
            HoldingsRecordId = holdingId;
        }
    }
}