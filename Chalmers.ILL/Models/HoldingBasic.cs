namespace Chalmers.ILL.Models
{
    public class HoldingBasic
    {
        public bool DiscoverySuppress { get; set; }
        public string InstanceId { get; set; }
        public string PermanentLocationId { get; set; }
        public string[] StatisticalCodeIds { get; set; }
        public string CallNumber { get; set; }
    }
}