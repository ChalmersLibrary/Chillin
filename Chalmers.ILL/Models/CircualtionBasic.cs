using System;

namespace Chalmers.ILL.Models
{
    public class CirculationBasic
    {
        public string ItemId { get; set; }
        public string RequesterId { get; set; }
        public string RequestType { get; set; } = "Page";
        public string RequestDate { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        public string FulfilmentPreference { get; set; } = "Hold Shelf";
        public string PickupServicePointId { get; set; }

        public CirculationBasic(string itemId, string requesterId, string pickupServicePoint)
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
            ItemId = itemId;
            RequesterId = requesterId;
            PickupServicePointId = pickupServicePoint;
        }
    }
}