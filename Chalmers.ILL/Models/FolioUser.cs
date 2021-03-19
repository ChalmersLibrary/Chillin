namespace Chalmers.ILL.Models
{
    public class FolioUser
    {
        public User[] Users { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
    }
}