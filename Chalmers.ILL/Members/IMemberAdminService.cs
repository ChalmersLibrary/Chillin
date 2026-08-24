using System.Collections.Generic;

namespace Chalmers.ILL.Members
{
    public interface IMemberAdminService
    {
        List<MemberAccount> GetAllMembers();
        void CreateMember(string login, string password, List<string> roles);
        void SetPassword(string login, string newPassword);
        void SetRoles(string login, List<string> roles);
        void DeleteMember(string login);
    }
}
