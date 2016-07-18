using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chalmers.ILL.Models
{
    // Main Order Item
    public class OrderItemModel
    {
        public OrderItemModel()
        {
            StatusId = -1;
            PreviousStatusId = -1;
            LastDeliveryStatusId = -1;
            TypeId = -1;
            DeliveryLibraryId = -1;
            CancellationReasonId = -1;
            PurchasedMaterialId = -1;
            SierraInfo = new SierraModel();
        }

        public string OrderId { get; set; }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NodeId { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime FollowUpDate { get; set; }
        public bool FollowUpDateIsDue { get; set; }

        public int ContentVersionsCount { get; set; }

        public string OriginalOrder { get; set; }
        public string Reference { get; set; }

        public int TypeId { get; set; }
        public string Type { get; set; }

        public int StatusId { get; set; }
        public string Status { get; set; }
        public string StatusString { get; set; }

        public int PreviousStatusId { get; set; }
        public string PreviousStatus { get; set; }
        public string PreviousStatusString { get; set; }

        public int LastDeliveryStatusId { get; set; }
        public string LastDeliveryStatus { get; set; }
        public string LastDeliveryStatusString { get; set; }

        public int DeliveryLibraryId { get; set; }
        public string DeliveryLibrary { get; set; }

        public int CancellationReasonId { get; set; }
        public string CancellationReason { get; set; }

        public int PurchasedMaterialId { get; set; }
        public string PurchasedMaterial { get; set; }

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
        public DateTime DeliveryDate { get; set; }
        public string BookId { get; set; }
        public bool ReadOnlyAtLibrary { get; set; }

        public List<LogItem> LogItemsList { get; set; }
        public List<OrderAttachment> AttachmentList { get; set; }
        public SierraModel SierraInfo { get; set; }
    }
}