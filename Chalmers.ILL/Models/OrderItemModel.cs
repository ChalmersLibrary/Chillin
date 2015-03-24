using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using umbraco.businesslogic;
using umbraco.cms.businesslogic.member;

namespace Chalmers.ILL.Models
{
    // Main Order Item
    public class OrderItemModel
    {
        [Required]
        public string OrderId { get; set; }
        public int NodeId { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime FollowUpDate { get; set; }
        public bool FollowUpDateIsDue { get; set; }

        public int ContentVersionsCount { get; set; }

        public string OriginalOrder { get; set; }
        public string Reference { get; set; }

        public int Type { get; set; }
        public string TypePrevalue { get; set; }

        public int Status { get; set; }
        public string StatusPrevalue { get; set; }
        public string StatusString { get; set; }

        public int DeliveryLibrary { get; set; }
        public string DeliveryLibraryPrevalue { get; set; }

        public int CancellationReason { get; set; }
        public string CancellationReasonPrevalue { get; set; }

        public int PurchasedMaterial { get; set; }
        public string PurchasedMaterialPrevalue { get; set; }

        public string PatronName { get; set; }
        public string PatronEmail { get; set; }
        public string PatronCardNo { get; set; }

        public string ProviderName { get; set; }
        public string ProviderOrderId { get; set; }
        public string ProviderInformation { get; set; }
        public DateTime ProviderDueDate { get; set; }

        public string EditedBy { get; set; }
        public string EditedByMemberName { get; set; }
        public bool EditedByCurrentMember { get; set; }

        public string Log { get; set; }
        public string Attachments { get; set; }
        public string SierraInfoStr { get; set; }

        public string DrmWarning { get; set; }
        public bool DeliveryLibrarySameAsHomeLibrary { get; set; }

        public DateTime DueDate { get; set; }
        public DateTime ArrivedAtInfodiskDate { get; set; }
        public string BookId { get; set; }

        public List<LogItem> LogItemsList { get; set; }
        public List<OrderAttachment> AttachmentList { get; set; }
        public SierraModel SierraInfo { get; set; }
    }

    public class OrderItemVersions
    {
        public List<OrderItemVersion> List { get; set; }
    }

    public class OrderItemVersion
    {
        public int NodeId { get; set; }
        public int VersionCount { get; set; }
    }
}