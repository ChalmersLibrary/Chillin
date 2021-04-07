using System;
using System.Collections.Generic;
using static System.Configuration.ConfigurationManager;

namespace Chalmers.ILL.Models
{
    public class InventoryItemBasic
    {
        public string Barcode { get; set; }
        public bool DiscoverySuppress { get; set; } = true;
        public MaterialType MaterialType { get; set; } = new MaterialType();
        public PermanentLoanType PermanentLoanType { get; set; }
        public string HoldingsRecordId { get; set; }
        public Status Status { get; set; } = new Status();
        public List<CirculationNote> CirculationNotes { get; set; } = new List<CirculationNote>();
        public string[] StatisticalCodeIds { get; set; } = new string[]
            {
                AppSettings["chillinStatisticalCodeId"]
            };

        public InventoryItemBasic(string barcode, string holdingsRecordId, bool readOnlyAtLibrary)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                throw new ArgumentNullException(nameof(barcode));
            }
            if (string.IsNullOrEmpty(holdingsRecordId))
            {
                throw new ArgumentNullException(nameof(holdingsRecordId));
            }
            Barcode = barcode;
            HoldingsRecordId = holdingsRecordId;
            PermanentLoanType = new PermanentLoanType
            {
                Id = readOnlyAtLibrary ?
                    AppSettings["itemPermanentLoanTypeIdInHouse"] :
                    AppSettings["itemPermanentLoanTypeId"]
            };
        }
    }

    public class Status
    {
        public string Name { get; set; } = "Available";
    }

    public class CirculationNote
    {
        public string NoteType { get; set; }
        public string Note { get; set; }
        public bool StaffOnly { get; set; } = true;
    }

    public class MaterialType
    {
        public string Id { get; set; } = AppSettings["itemMaterialTypeId"];
    }

    public class PermanentLoanType
    {
        public string Id { get; set; }  
    }
}