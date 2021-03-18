namespace Chalmers.ILL.Models
{
    public class Holding
    {
        public string Id { get; set; }
        public string Hrid { get; set; }
        public object[] FormerIds { get; set; }
        public string InstanceId { get; set; }
        public string PermanentLocationId { get; set; }
        public object[] ElectronicAccess { get; set; }
        public object[] Notes { get; set; }
        public Holdingsstatement[] HoldingsStatements { get; set; }
        public object[] HoldingsStatementsForIndexes { get; set; }
        public object[] HoldingsStatementsForSupplements { get; set; }
        public object[] StatisticalCodeIds { get; set; }
        public object[] HoldingsItems { get; set; }
        public object[] BareHoldingsItems { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class Holdingsstatement
    {
        public string Statement { get; set; }
        public string Note { get; set; }
    }

}