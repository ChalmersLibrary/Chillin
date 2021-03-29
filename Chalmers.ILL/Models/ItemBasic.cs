using System.Collections.Generic;

namespace Chalmers.ILL.Models
{
    public class ItemBasic
    {
        public bool DiscoverySuppress { get; set; }
        public string MaterialTypeId { get; set; }
        public string PermanentLoanTypeId { get; set; }
        public string HoldingsRecordId { get; set; }
        public Status Status { get; set; }
        public string Barcode { get; set; }
        public List<CirculationNote> CirculationNotes { get; set; }
        public string[] StatisticalCodeIds { get; set; }
    }

    public class Status
    {
        public string Name { get; set; }
    }

    public class CirculationNote
    {
        public string NoteType { get; set; }
        public string Note { get; set; }
        public bool StaffOnly { get; set; }
    }
}