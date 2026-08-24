using System.Collections.Generic;
using System.Web.Helpers;
using Chalmers.ILL.Members;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.Members
{
    [TestClass]
    public class FileMembershipProviderTest
    {
        [TestMethod]
        public void ValidateUser_CorrectPassword_ReturnsTrue()
        {
            var provider = MakeProvider(new SaveCapture(), Account("alice", "correct-horse"));

            Assert.IsTrue(provider.ValidateUser("alice", "correct-horse"));
        }

        [TestMethod]
        public void ValidateUser_WrongPassword_ReturnsFalse()
        {
            var provider = MakeProvider(new SaveCapture(), Account("alice", "correct-horse"));

            Assert.IsFalse(provider.ValidateUser("alice", "wrong-password"));
        }

        [TestMethod]
        public void ValidateUser_UnknownUser_ReturnsFalse()
        {
            var provider = MakeProvider(new SaveCapture(), Account("alice", "correct-horse"));

            Assert.IsFalse(provider.ValidateUser("bob", "correct-horse"));
        }

        [TestMethod]
        public void ValidateUser_LoginIsCaseInsensitive()
        {
            var provider = MakeProvider(new SaveCapture(), Account("alice", "correct-horse"));

            Assert.IsTrue(provider.ValidateUser("ALICE", "correct-horse"));
        }

        // GetUser for a known user isn't unit-testable in isolation: MembershipUser's constructor
        // validates its providerName against the globally registered Membership.Providers
        // collection, which is only populated by ASP.NET at app startup from Web.config.

        [TestMethod]
        public void GetUser_UnknownUser_ReturnsNull()
        {
            var provider = MakeProvider(new SaveCapture(), Account("alice", "correct-horse"));

            Assert.IsNull(provider.GetUser("bob", false));
        }

        [TestMethod]
        public void ChangePassword_CorrectOldPassword_SavesNewHashAndReturnsTrue()
        {
            var capture = new SaveCapture();
            var provider = MakeProvider(capture, Account("alice", "correct-horse"));

            var result = provider.ChangePassword("alice", "correct-horse", "new-password");

            Assert.IsTrue(result);
            Assert.IsNotNull(capture.Saved);
            Assert.IsTrue(Crypto.VerifyHashedPassword(capture.Saved[0].PasswordHash, "new-password"));
        }

        [TestMethod]
        public void ChangePassword_WrongOldPassword_ReturnsFalseAndDoesNotSave()
        {
            var capture = new SaveCapture();
            var provider = MakeProvider(capture, Account("alice", "correct-horse"));

            var result = provider.ChangePassword("alice", "wrong-password", "new-password");

            Assert.IsFalse(result);
            Assert.IsNull(capture.Saved);
        }

        private static MemberAccount Account(string login, string password) =>
            new MemberAccount { Login = login, PasswordHash = Crypto.HashPassword(password), Roles = new List<string>() };

        private static FileMembershipProvider MakeProvider(SaveCapture capture, params MemberAccount[] accounts)
        {
            return new FileMembershipProvider(
                () => new List<MemberAccount>(accounts),
                a => capture.Saved = a);
        }

        private class SaveCapture
        {
            public List<MemberAccount> Saved;
        }
    }
}
