using System;
using static System.Configuration.ConfigurationManager;

namespace Chalmers.ILL.Models
{
    public class InstanceBasic
    {
        public string Title { get; set; }
        public string Source { get; set; } = "FOLIO";
        public string InstanceTypeId { get; set; } = AppSettings["instanceResourceTypeId"];
        public bool DiscoverySuppress { get; set; } = true;
        public string StatusId { get; set; } = AppSettings["instanceStatusId"];
        public string ModeOfIssuanceId { get; set; } = AppSettings["instanceModesOfIssuance"];
        public Identifier[] Identifiers { get; set; }
        public string[] StatisticalCodeIds { get; set; } = new string[]
            {
                AppSettings["chillinStatisticalCodeId"]
            };

        public InstanceBasic(string title, string orderId)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException(nameof(title));
            }
            if (string.IsNullOrEmpty(orderId))
            {
                throw new ArgumentNullException(nameof(orderId));
            }
            Title = title;
            Identifiers = new Identifier[]
            {
                new Identifier
                {
                    Value = orderId,
                    IdentifierTypeId = AppSettings["instanceIdentifierTypeId"]
                }
            };
        }
    }
}