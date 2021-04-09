namespace Chalmers.ILL.Models
{

    public class FolioUserNameQuery
    {
        public FolioUser[] Users { get; set; }
        public int TotalRecords { get; set; }
    }

    public class FolioUser
    {
        public string Id { get; set; }
        public Personal Personal { get; set; }
    }
}
