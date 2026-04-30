using System;
using System.Security;
using CredentialManagement;
using Shouldly;
using UiPath.Credentials.Activities.API;
using Xunit;

namespace UiPath.Credentials.Activities.API.Tests
{
    public class CredentialsServiceTests
    {
        private readonly CredentialsService _service = new CredentialsService();

        #region Guard clauses

        [Fact]
        public void GetCredential_NullTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.GetCredential(null));
        }

        [Fact]
        public void GetCredential_EmptyTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.GetCredential(string.Empty));
        }

        [Fact]
        public void GetSecureCredential_NullTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.GetSecureCredential(null));
        }

        [Fact]
        public void AddCredential_NullTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.AddCredential(null, "user", "pass"));
        }

        [Fact]
        public void AddCredential_NullPassword_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.AddCredential("target", "user", (string)null));
        }

        [Fact]
        public void AddCredentialSecure_NullTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.AddCredential(null, "user", new SecureString()));
        }

        [Fact]
        public void AddCredentialSecure_NullPassword_Throws()
        {
            Should.Throw<ArgumentNullException>(() => _service.AddCredential("target", "user", (SecureString)null));
        }

        [Fact]
        public void DeleteCredential_NullTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.DeleteCredential(null));
        }

        [Fact]
        public void DeleteCredential_WhitespaceTarget_Throws()
        {
            Should.Throw<ArgumentException>(() => _service.DeleteCredential("   "));
        }

        #endregion

        #region GetCredential — not found

        [Fact]
        public void GetCredential_NonExistentTarget_ReturnsFalse()
        {
            var result = _service.GetCredential("UiPath.Test.NonExistent." + Guid.NewGuid());

            result.Found.ShouldBeFalse();
            result.Username.ShouldBeNull();
            result.Password.ShouldBeNull();
        }

        [Fact]
        public void GetSecureCredential_NonExistentTarget_ReturnsFalse()
        {
            var result = _service.GetSecureCredential("UiPath.Test.NonExistent." + Guid.NewGuid());

            result.Found.ShouldBeFalse();
            result.Username.ShouldBeNull();
            result.Password.ShouldBeNull();
        }

        #endregion

        #region AddCredential / GetCredential round-trip

        [Fact]
        public void AddCredential_ThenGetCredential_ReturnsStoredValues()
        {
            string target = "UiPath.Test." + Guid.NewGuid();
            try
            {
                bool saved = _service.AddCredential(target, "testuser", "testpass");
                saved.ShouldBeTrue();

                var result = _service.GetCredential(target);

                result.Found.ShouldBeTrue();
                result.Username.ShouldBe("testuser");
                result.Password.ShouldBe("testpass");
            }
            finally
            {
                _service.DeleteCredential(target);
            }
        }

        [Fact]
        public void AddCredentialSecure_ThenGetCredential_ReturnsStoredValues()
        {
            string target = "UiPath.Test." + Guid.NewGuid();
            try
            {
                var secure = new SecureString();
                foreach (char c in "securepass")
                    secure.AppendChar(c);
                secure.MakeReadOnly();

                bool saved = _service.AddCredential(target, "secureuser", secure);
                saved.ShouldBeTrue();

                var result = _service.GetCredential(target);

                result.Found.ShouldBeTrue();
                result.Username.ShouldBe("secureuser");
                result.Password.ShouldBe("securepass");
            }
            finally
            {
                _service.DeleteCredential(target);
            }
        }

        #endregion

        #region DeleteCredential

        [Fact]
        public void DeleteCredential_AfterAdd_RemovesEntry()
        {
            string target = "UiPath.Test." + Guid.NewGuid();
            _service.AddCredential(target, "user", "pass");

            bool deleted = _service.DeleteCredential(target);

            deleted.ShouldBeTrue();
            var result = _service.GetCredential(target);
            result.Found.ShouldBeFalse();
        }

        [Fact]
        public void DeleteCredential_NonExistent_ReturnsFalse()
        {
            var deleted = _service.DeleteCredential("UiPath.Test.NonExistent." + Guid.NewGuid());

            deleted.ShouldBeFalse();
        }

        #endregion
    }
}
