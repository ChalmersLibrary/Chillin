using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;

namespace Chalmers.ILL.Members
{
    // Backs the SuperAdmin account management page. Reuses the same MemberFileStore that
    // FileMembershipProvider/FileRoleProvider read at login time, so changes made here take
    // effect immediately without an app restart.
    public class MemberAdminService : IMemberAdminService
    {
        private readonly Func<List<MemberAccount>> _load;
        private readonly Action<List<MemberAccount>> _save;

        public MemberAdminService() : this(MemberFileStore.Load, MemberFileStore.Save) { }

        public MemberAdminService(Func<List<MemberAccount>> load, Action<List<MemberAccount>> save)
        {
            _load = load;
            _save = save;
        }

        public List<MemberAccount> GetAllMembers() => _load();

        public void CreateMember(string login, string password, List<string> roles)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Inloggningsnamn saknas.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Lösenord saknas.");

            var accounts = _load();
            if (accounts.Any(a => IsMatch(a, login)))
                throw new InvalidOperationException($"Kontot \"{login}\" finns redan.");

            accounts.Add(new MemberAccount
            {
                Login = login,
                PasswordHash = Crypto.HashPassword(password),
                Roles = roles ?? new List<string>()
            });
            _save(accounts);
        }

        public void SetPassword(string login, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Lösenord saknas.");

            var accounts = _load();
            var account = FindOrThrow(accounts, login);
            account.PasswordHash = Crypto.HashPassword(newPassword);
            _save(accounts);
        }

        public void SetRoles(string login, List<string> roles)
        {
            var accounts = _load();
            var account = FindOrThrow(accounts, login);
            account.Roles = roles ?? new List<string>();
            _save(accounts);
        }

        public void DeleteMember(string login)
        {
            var accounts = _load();
            var account = FindOrThrow(accounts, login);
            accounts.Remove(account);
            _save(accounts);
        }

        private static MemberAccount FindOrThrow(List<MemberAccount> accounts, string login)
        {
            var account = accounts.FirstOrDefault(a => IsMatch(a, login));
            if (account == null)
                throw new InvalidOperationException($"Kontot \"{login}\" hittades inte.");
            return account;
        }

        private static bool IsMatch(MemberAccount account, string login) =>
            string.Equals(account.Login, login, StringComparison.OrdinalIgnoreCase);
    }
}
