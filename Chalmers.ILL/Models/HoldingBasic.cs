using System;
using static System.Configuration.ConfigurationManager;

namespace Chalmers.ILL.Models
{
    public class HoldingBasic
    {
        public string CallNumber { get; set; } = "Interlibrary-in-loan";
        public bool DiscoverySuppress { get; set; } = true;
        public string InstanceId { get; set; }
        public string SourceId { get; set; } = AppSettings["folioSourceId"];
        public string PermanentLocationId { get; set; } = AppSettings["holdingPermanentLocationId"];
        public string[] StatisticalCodeIds { get; set; } = new string[]
            {
                AppSettings["chillinStatisticalCodeId"]
            };

        public HoldingBasic(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                throw new ArgumentNullException(nameof(instanceId));
            }
            InstanceId = instanceId;
        }
    }
}