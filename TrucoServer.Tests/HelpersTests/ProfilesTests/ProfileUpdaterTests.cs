using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Profiles;

namespace TrucoServer.Tests.HelpersTests.ProfilesTests
{
    [TestClass]
    public class ProfileUpdaterTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<DbSet<User>> mockUserSet;
        private Mock<DbSet<UserProfile>> mockProfileSet;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockUserSet = new Mock<DbSet<User>>();
            mockProfileSet = new Mock<DbSet<UserProfile>>();

            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            mockContext.Setup(c => c.UserProfile).Returns(mockProfileSet.Object);
        }

        [TestMethod]
        public void TestTryUpdateUsernameReturnsFalseIfMaxChangesReached()
        {
            var updater = new ProfileUpdater(mockContext.Object);
            
            var user = new User 
            {
                username = "OldName", 
                nameChangeCount = 3
            };

            var context = new UsernameUpdateContext 
            {
                User = user,
                NewUsername = "NewName",
                MaxNameChanges = 3 
            };

            bool result = updater.TryUpdateUsername(context);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryUpdateUsernameReturnsFalseIfUsernameTakenByAnother()
        {
            var updater = new ProfileUpdater(mockContext.Object);
           
            var currentUser = new User 
            { userID = 1, 
                username = "Me", 
                nameChangeCount = 0
            };

            var otherUser = new User 
            { 
                userID = 2, 
                username = "TakenName"
            };

            var updateContext = new UsernameUpdateContext 
            { 
                User = currentUser,
                NewUsername = "TakenName", 
                MaxNameChanges = 5 
            };

            var data = new List<User> 
            { 
                otherUser 
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            bool result = updater.TryUpdateUsername(updateContext);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUpdateProfileDetailsThrowsArgumentNullExceptionForNullData()
        {
            var updater = new ProfileUpdater(mockContext.Object);
            var user = new User();

            var options = new ProfileUpdateOptions 
            { 
                ProfileData = null 
            };

            Assert.ThrowsException<ArgumentNullException>(() => updater.UpdateProfileDetails(user, options));
        }

        [TestMethod]
        public void TestUpdateProfileDetailsCreatesProfileIfMissing()
        {
            var updater = new ProfileUpdater(mockContext.Object);

            var user = new User 
            {
                userID = 10, 
                UserProfile = null
            };

            var options = new ProfileUpdateOptions
            {
                ProfileData = new UserProfileData(),
                DefaultLanguageCode = "en-US",
                DefaultAvatarId = "avatar_aaa_default"
            };

            updater.UpdateProfileDetails(user, options);
            mockProfileSet.Verify(m => m.Add(It.IsAny<UserProfile>()), Times.Once);
        }

        [TestMethod]
        public void TestProcessAvatarUpdateReturnsFalseIfUserNotFound()
        {
            var updater = new ProfileUpdater(mockContext.Object);
            var data = new List<User>().AsQueryable(); 
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            bool result = updater.ProcessAvatarUpdate("TestUser", "avatar_aaa_default");
            Assert.IsFalse(result);
        }
    }
}
