using Newtonsoft.Json;

namespace Chalmers.ILL.Models
{

    public class HoldingBasic
    {
        [JsonProperty("instanceId")]
        public string InstanceId { get; set; }

        [JsonProperty("permanentLocationId")]
        public string PermanentLocationId { get; set; }
    }
}