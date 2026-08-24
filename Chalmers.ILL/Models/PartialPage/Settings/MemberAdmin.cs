using System.Collections.Generic;

namespace Chalmers.ILL.Models.PartialPage.Settings
{
    public class MemberAdmin
    {
        public List<MemberSummary> Members { get; set; } = new List<MemberSummary>();
    }

    public class MemberSummary
    {
        public string Login { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
