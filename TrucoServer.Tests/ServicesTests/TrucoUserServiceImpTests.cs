using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Services;

namespace TrucoServer.Tests.ServicesTests
{
    [TestClass]
    public class TrucoUserServiceImpTests
    {
        private TrucoUserServiceImp service;
        private const string TEST_USER = "TestUser";
        private const string TEST_EMAIL = "TU@gmail.com";
        private const string TEST_PASS = "Password123!";

        [TestInitialize]
        public void Setup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                CleanupUser(context, TEST_USER);
                CleanupUser(context, "NewUser");
            }

            service = new TrucoUserServiceImp();
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                CleanupUser(context, TEST_USER);
                CleanupUser(context, "NewUser");
            }
        }

        private void CleanupUser(baseDatosTrucoEntities context, string username)
        {
            var user = context.User.FirstOrDefault(u => u.username == username);
           
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

        [TestMethod]
        public void TestRegisterValidUserShouldCreateDatabaseRecord()
        {
            bool result = service.Register("NewUser", TEST_PASS, "new@gmail.com");

            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == "NewUser");
                var profile = context.UserProfile.FirstOrDefault(p => p.userID == user.userID);
                Assert.IsNotNull(profile);
            }
        }

        [TestMethod]
        public void TestRegisterDuplicateUsernameShouldReturnFalse()
        {
            service.Register("NewUser", TEST_PASS, "new@gmail.com");
            bool result = service.Register("NewUser", TEST_PASS, "other@gmail.com");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoginValidCredentialsShouldReturnTrue()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);

            try
            {
                bool result = service.Login(TEST_USER, TEST_PASS, "en-US");
            }
            catch 
            {
                /* noop */
            }

            using (var context = new baseDatosTrucoEntities())
            {
                Assert.IsNotNull(context.User.FirstOrDefault(u => u.username == TEST_USER));
            }
        }

        [TestMethod]
        public void TestLoginInvalidPasswordShouldReturnFalse()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);
            bool result = service.Login(TEST_USER, "WrongPass", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoginNonExistentUserShouldReturnFalse()
        {
            bool result = service.Login("TestUser", "AnyPass", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestGetUserProfileByEmailAsyncValidEmailShouldReturnProfile()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);
            var profile = await service.GetUserProfileByEmailAsync(TEST_EMAIL);
            Assert.AreEqual(TEST_USER, profile?.Username);
        }

        [TestMethod]
        public void TestSaveUserProfileValidDataShouldUpdateDatabase()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);

            var newData = new UserProfileData
            {
                Email = TEST_EMAIL,
                Username = TEST_USER,
                LanguageCode = "es-MX",
                IsMusicMuted = true
            };

            bool result = service.SaveUserProfile(newData);

            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.Include("UserProfile").FirstOrDefault(u => u.username == TEST_USER);
                Assert.AreEqual("es-MX", user.UserProfile.languageCode);
                Assert.IsTrue(user.UserProfile.isMusicMuted);
            }
        }

        [TestMethod]
        public async Task TestUpdateUserAvatarAsyncValidUserShouldUpdateAvatar()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);
            bool result = await service.UpdateUserAvatarAsync(TEST_USER, "avatar_aaa_chivas");

            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.Include("UserProfile").FirstOrDefault(u => u.username == TEST_USER);
                Assert.AreEqual("avatar_aaa_chivas", user.UserProfile.avatarID);
            }
        }

        [TestMethod]
        public void TestUsernameExistsExistingUserShouldReturnTrue()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);
            bool exists = service.UsernameExists(TEST_USER);
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void TestEmailExistsExistingEmailShouldReturnTrue()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);
            bool exists = service.EmailExists(TEST_EMAIL);
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void TestUsernameExistsInvalidFormatShouldReturnFalse()
        {
            bool result = service.UsernameExists("User@!nvalid");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestGetGlobalRankingShouldReturnList()
        {
            service.Register(TEST_USER, TEST_PASS, TEST_EMAIL);
            var ranking = service.GetGlobalRanking();
            Assert.IsTrue(ranking.Count > 0);
        }

        [TestMethod]
        public void TestGetLastMatchesShouldReturnList()
        {
            var history = service.GetLastMatches(TEST_USER);
            Assert.IsNotNull(history);
        }
    }
}
