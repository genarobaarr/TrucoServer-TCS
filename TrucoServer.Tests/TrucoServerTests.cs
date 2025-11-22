using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace TrucoServer.Services.Tests
{
    [TestClass]
    public class TrucoServerTests
    {
        private TrucoServer server;
        private const string TEST_USER = "AuthTestUser";
        private const string TEST_USER_2 = "AuthTestUser2";
        private const string TEST_UNKNOWN_USER = "UnknownUser";
        private const string TEST_USER_NON_EXISTENT = "NonExistentUser";
        private const string TEST_EMAIL = "authtest@example.com";
        private const string TEST_EMAIL_2 = "other@example.com";
        private const string TEST_PASSWORD = "Password123!";
        private const string TEST_NEW_PASSWORD = "NewPassword456!";
        private const string TEST_WRONG_PASSWORD = "WrongPassword";
        private const string TEST_LANGUAGE = "en";

        [TestInitialize]
        public void Setup()
        {
            server = new TrucoServer();
            CleanDatabase();
        }

        [TestCleanup]
        public void Cleanup()
        {
            CleanDatabase();
        }

        private void CleanDatabase()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == TEST_USER || u.email == TEST_EMAIL);

                if (user != null)
                {
                    var profile = context.UserProfile.FirstOrDefault(p => p.userID == user.userID);

                    if (profile != null)
                    {
                        context.UserProfile.Remove(profile);
                    }

                    context.User.Remove(user);
                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestRegisterReturnsTrueForNewUser()
        {
            bool result = server.Register(TEST_USER, TEST_PASSWORD, TEST_EMAIL);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRegisterCreatesUserInDatabase()
        {
            server.Register(TEST_USER, TEST_PASSWORD, TEST_EMAIL);
            using (var context = new baseDatosTrucoEntities())
            {
                bool exists = context.User.Any(u => u.username == TEST_USER);
                Assert.IsTrue(exists);
            }
        }

        [TestMethod]
        public void TestRegisterCreatesUserProfileInDatabase()
        {
            server.Register(TEST_USER, TEST_PASSWORD, TEST_EMAIL);
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == TEST_USER);
                bool profileExists = context.UserProfile.Any(p => p.userID == user.userID);
                Assert.IsTrue(profileExists);
            }
        }

        [TestMethod]
        public void TestRegisterReturnsFalseForExistingUsername()
        {
            server.Register(TEST_USER, TEST_PASSWORD, TEST_EMAIL_2);
            bool result = server.Register(TEST_USER, TEST_NEW_PASSWORD, TEST_EMAIL);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRegisterReturnsFalseForExistingEmail()
        {
            server.Register(TEST_USER_2, TEST_PASSWORD, TEST_EMAIL);
            bool result = server.Register(TEST_USER, TEST_NEW_PASSWORD, TEST_EMAIL);
            using (var context = new baseDatosTrucoEntities())
            {
                var extra = context.User.FirstOrDefault(u => u.username == TEST_USER_2);
                if (extra != null)
                {
                    var p = context.UserProfile.FirstOrDefault(pro => pro.userID == extra.userID);
                    if (p != null) context.UserProfile.Remove(p);
                    context.User.Remove(extra);
                    context.SaveChanges();
                }
            }
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoginReturnsFalseForNonExistentUser()
        {
            bool result = server.Login(TEST_USER_NON_EXISTENT, TEST_PASSWORD, TEST_LANGUAGE);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoginReturnsFalseForWrongPassword()
        {
            server.Register(TEST_USER, TEST_PASSWORD, TEST_EMAIL);
            bool result = server.Login(TEST_USER, TEST_WRONG_PASSWORD, TEST_LANGUAGE);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsTrueForRegisteredUser()
        {
            server.Register(TEST_USER, TEST_PASSWORD, TEST_EMAIL);
            bool result = server.UsernameExists(TEST_USER);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsFalseForUnregisteredUser()
        {
            bool result = server.UsernameExists(TEST_UNKNOWN_USER);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsFalseForNullInput()
        {
            bool result = server.UsernameExists(null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsFalseForEmptyInput()
        {
            bool result = server.UsernameExists("   ");
            Assert.IsFalse(result);
        }
    }
}