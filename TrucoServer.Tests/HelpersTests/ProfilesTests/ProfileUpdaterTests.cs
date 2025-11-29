using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests.HelpersTests.ProfilesTests
{
    [TestClass]
    public class ProfileUpdaterTests
    {
        private Helpers.Profiles.ProfileUpdater updater;
        private User testUser;

        [TestInitialize]
        public void Setup()
        {
            updater = new Helpers.Profiles.ProfileUpdater();
            testUser = new User
            {
                username = "TestUser",
                nameChangeCount = 0,
                userID = 1,
                UserProfile = new UserProfile 
                { 
                    avatarID = "avatar_aaa_default", 
                    languageCode = "en-US" 
                }
            };
        }

        [TestMethod]
        public void TestValidateProfileInputValidDataShouldReturnTrue()
        {
            var profile = new Data.DTOs.UserProfileData
            { 
                Username = "NewName",
                Email = "valid@email.com" 
            };

            bool result = updater.ValidateProfileInput(profile);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestValidateProfileInputNullProfileShouldReturnFalse()
        {
            bool result = updater.ValidateProfileInput(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestValidateProfileInputInvalidEmailShouldReturnFalse()
        {
            var profile = new Data.DTOs.UserProfileData 
            { 
                Username = "ValidName", 
                Email = "invalid-email" 
            };

            bool result = updater.ValidateProfileInput(profile);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryUpdateUsernameSameNameShouldReturnTrue()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                bool result = updater.TryUpdateUsername(context, testUser, "TestUser", 3);
                Assert.IsTrue(result);
                Assert.AreEqual(0, testUser.nameChangeCount);
            }
        }

        [TestMethod]
        public void TestTryUpdateUsernameMaxChangesReachedShouldReturnFalse()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                testUser.nameChangeCount = 3;
                bool result = updater.TryUpdateUsername(context, testUser, "NewName", 3);
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void TestProcessAvatarUpdateNonExistentUserShouldReturnFalse()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                bool result = updater.ProcessAvatarUpdate(context, "GhostUser", "avatar_aaa_chivas");
                Assert.IsFalse(result);
            }
        }
    }
}
