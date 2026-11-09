using System.Collections.Generic;

namespace Chalmers.ILL.Members
{
    public class MemberAccount
    {
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
