using Newtonsoft.Json;

namespace Chalmers.ILL.Models
{
    public class HoldingBasic
    {
        public bool DiscoverySuppress { get; set; }
        public string InstanceId { get; set; }
        public string PermanentLocationId { get; set; }
    }
}