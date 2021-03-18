namespace Chalmers.ILL.Models
{
    public class Item
    {
        public string Id { get; set; }
        public string Hrid { get; set; }
        public string HoldingsRecordId { get; set; }
        public object[] FormerIds { get; set; }
        public string Barcode { get; set; }
        public object[] YearCaption { get; set; }
        public object[] Notes { get; set; }
        public object[] CirculationNotes { get; set; }
        public Status Status { get; set; }
        public string MaterialTypeId { get; set; }
        public string PermanentLoanTypeId { get; set; }
        public string EffectiveLocationId { get; set; }
        public object[] ElectronicAccess { get; set; }
        public object[] StatisticalCodeIds { get; set; }
        public Metadata Metadata { get; set; }
    }
}