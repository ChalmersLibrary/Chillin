namespace Chalmers.ILL.Models
{
    public class InstanceBasic
    {
        public string Title { get; set; }
        public string Source { get; set; }
        public string InstanceTypeId { get; set; }
        public bool DiscoverySuppress { get; set; }
        public string StatusId { get; set; }
        public string ModeOfIssuanceId { get; set; }
        public Identifier[] Identifiers { get; set; }
    }
}