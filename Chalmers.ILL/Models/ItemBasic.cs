namespace Chalmers.ILL.Models
{
    public class ItemBasic
    {
        public string MaterialTypeId { get; set; }
        public string PermanentLoanTypeId { get; set; }
        public string HoldingsRecordId { get; set; }
        public Status Status { get; set; }
        public string Barcode { get; set; }
    }

    public class Status
    {
        public string Name { get; set; }
    }
}