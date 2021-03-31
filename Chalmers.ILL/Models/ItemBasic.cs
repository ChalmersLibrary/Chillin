using System;
using System.Collections.Generic;
using static System.Configuration.ConfigurationManager;

namespace Chalmers.ILL.Models
{
    public class ItemBasic
    {
        public string Barcode { get; set; }
        public bool DiscoverySuppress { get; set; } = true;
        public string MaterialTypeId { get; set; } = AppSettings["itemMaterialTypeId"];
        public string PermanentLoanTypeId { get; set; }
        public string HoldingsRecordId { get; set; }
        public Status Status { get; set; } = new Status();
        public List<CirculationNote> CirculationNotes { get; set; } = new List<CirculationNote>();
        public string[] StatisticalCodeIds { get; set; } = new string[]
            {
                AppSettings["chillinStatisticalCodeId"]
            };

        public ItemBasic(string barcode, string holdingsRecordId, bool readOnlyAtLibrary)
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
            PermanentLoanTypeId = readOnlyAtLibrary ?
                AppSettings["itemPermanentLoanTypeIdInHouse"] :
                AppSettings["itemPermanentLoanTypeId"];
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
}