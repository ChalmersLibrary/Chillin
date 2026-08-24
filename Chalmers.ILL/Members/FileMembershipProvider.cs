using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Helpers;
using System.Web.Security;

namespace Chalmers.ILL.Members
{
    // Minimal MembershipProvider backed by MemberFileStore instead of a database. Only the
    // members actually used elsewhere in the app (ValidateUser, GetUser, ChangePassword) are
    // implemented; everything else (self-registration, password reset by email, etc.) isn't
    // needed for a handful of manually managed accounts and throws NotSupportedException.
    public class FileMembershipProvider : MembershipProvider
    {
        private readonly Func<List<MemberAccount>> _loadAccounts;
        private readonly Action<List<MemberAccount>> _saveAccounts;

        public FileMembershipProvider() : this(MemberFileStore.Load, MemberFileStore.Save) { }

        public FileMembershipProvider(Func<List<MemberAccount>> loadAccounts, Action<List<MemberAccount>> saveAccounts)
        {
            _loadAccounts = loadAccounts;
            _saveAccounts = saveAccounts;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(string.IsNullOrEmpty(name) ? "FileMembershipProvider" : name, config ?? new NameValueCollection());
        }

        public override string ApplicationName { get; set; } = "Chillin";

        public override bool ValidateUser(string username, string password)
        {
            var account = FindAccount(username);
            return account != null && !string.IsNullOrEmpty(account.PasswordHash) && Crypto.VerifyHashedPassword(account.PasswordHash, password);
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            var account = FindAccount(username);
            if (account == null) return null;

            return new MembershipUser(Name, account.Login, null, "", "", "", true, false,
                DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var accounts = _loadAccounts();
            var account = accounts.FirstOrDefault(a => IsMatch(a, username));
            if (account == null || string.IsNullOrEmpty(account.PasswordHash) || !Crypto.VerifyHashedPassword(account.PasswordHash, oldPassword))
                return false;

            account.PasswordHash = Crypto.HashPassword(newPassword);
            _saveAccounts(accounts);
            return true;
        }

        private MemberAccount FindAccount(string username) => _loadAccounts().FirstOrDefault(a => IsMatch(a, username));

        private static bool IsMatch(MemberAccount account, string username) =>
            string.Equals(account.Login, username, StringComparison.OrdinalIgnoreCase);

        public override bool EnablePasswordRetrieval => false;
        public override bool EnablePasswordReset => false;
        public override bool RequiresQuestionAndAnswer => false;
        public override bool RequiresUniqueEmail => false;
        public override MembershipPasswordFormat PasswordFormat => MembershipPasswordFormat.Hashed;
        public override int MaxInvalidPasswordAttempts => int.MaxValue;
        public override int MinRequiredNonAlphanumericCharacters => 0;
        public override int MinRequiredPasswordLength => 6;
        public override int PasswordAttemptWindow => 0;
        public override string PasswordStrengthRegularExpression => "";

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer) =>
            throw new NotSupportedException();

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status) =>
            throw new NotSupportedException();

        public override bool DeleteUser(string username, bool deleteAllRelatedData) => throw new NotSupportedException();

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords) =>
            throw new NotSupportedException();

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords) =>
            throw new NotSupportedException();

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords) =>
            throw new NotSupportedException();

        public override int GetNumberOfUsersOnline() => throw new NotSupportedException();

        public override string GetPassword(string username, string answer) => throw new NotSupportedException();

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline) => throw new NotSupportedException();

        public override string GetUserNameByEmail(string email) => throw new NotSupportedException();

        public override string ResetPassword(string username, string answer) => throw new NotSupportedException();

        public override void UpdateUser(MembershipUser user) => throw new NotSupportedException();

        public override bool UnlockUser(string userName) => throw new NotSupportedException();
    }
}
