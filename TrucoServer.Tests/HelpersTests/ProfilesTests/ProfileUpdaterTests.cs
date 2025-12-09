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

        private const int USER_ID = 1;
        private const int MAX_CHANGE_NAME = 3;
        private const int ZERO_CHANGE_NAME = 0;
        private const int SECOND_USER_ID = 2;
        private const int THIRD_USER_ID = 10;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
        }

        private static Mock<DbSet<T>> GetMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            mockSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => sourceList.Add(s));

            return mockSet;
        }

        [TestMethod]
        public void TestTryUpdateUsernameReturnsFalseIfMaxChangesReached()
        {
            var updater = new ProfileUpdater(mockContext.Object);
            
            var user = new User 
            {
                username = "OldName", 
                nameChangeCount = MAX_CHANGE_NAME
            };

            var context = new UsernameUpdateContext 
            {
                User = user,
                NewUsername = "NewName",
                MaxNameChanges = MAX_CHANGE_NAME
            };

            bool result = updater.TryUpdateUsername(context);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryUpdateUsernameReturnsFalseIfUsernameTakenByAnother()
        {
            var currentUser = new User
            {
                userID = USER_ID,
                username = "Me",
                nameChangeCount = ZERO_CHANGE_NAME
            };

            var otherUser = new User
            {
                userID = SECOND_USER_ID,
                username = "TakenName"
            };

            var userList = new List<User>
            {
                otherUser 
            }; 

            var mockUserSet = GetMockDbSet(userList);
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            var updater = new ProfileUpdater(mockContext.Object);

            var updateContext = new UsernameUpdateContext
            {
                User = currentUser,
                NewUsername = "TakenName",
                MaxNameChanges = MAX_CHANGE_NAME
            };

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

            Assert.ThrowsException<InvalidOperationException>(() => updater.UpdateProfileDetails(user, options));
        }

        [TestMethod]
        public void TestUpdateProfileDetailsCreatesProfileIfMissing()
        {
            var profileList = new List<UserProfile>();
            var mockProfileSet = GetMockDbSet(profileList);
            mockContext.Setup(c => c.UserProfile).Returns(mockProfileSet.Object);
            var updater = new ProfileUpdater(mockContext.Object);

            var user = new User
            {
                userID = THIRD_USER_ID,
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
            var userList = new List<User>();
            var mockUserSet = GetMockDbSet(userList);
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            var updater = new ProfileUpdater(mockContext.Object);
            bool result = updater.ProcessAvatarUpdate("TestUser", "avatar_aaa_default");
            Assert.IsFalse(result);
        }
    }
}
