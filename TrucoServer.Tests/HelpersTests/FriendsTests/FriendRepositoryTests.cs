using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Friends;

namespace TrucoServer.Tests.HelpersTests.FriendsTests
{
    [TestClass]
    public class FriendRepositoryTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;

        private const int USER_ID = 2;
        private const int FRIEND_ID = 1;

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
            mockSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((s) => sourceList.Remove(s));

            return mockSet;
        }

        [TestMethod]
        public void TestConstructorThrowsExceptionForNullContext()
        {
            baseDatosTrucoEntities ctx = null;
            Assert.ThrowsException<ArgumentNullException>(() => new FriendRepository(ctx));
        }

        [TestMethod]
        public void TestGetUsersFromDatabaseThrowsForNullOptions()
        {
            var repo = new FriendRepository(mockContext.Object);
            Assert.ThrowsException<ArgumentNullException>(() => repo.GetUsersFromDatabase(null));
        }

        [TestMethod]
        public void TestCheckFriendshipExistsReturnsTrueForReciprocal()
        {
            var friendshipList = new List<Friendship>
            {
                new Friendship
                {
                    userID = USER_ID,
                    friendID = FRIEND_ID
                }
            };

            var mockFriendshipSet = GetMockDbSet(friendshipList);
            mockContext.Setup(c => c.Friendship).Returns(mockFriendshipSet.Object);
            var repo = new FriendRepository(mockContext.Object);
            bool exists = repo.CheckFriendshipExists(FRIEND_ID, USER_ID);
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void TestCommitFriendshipAcceptanceThrowsForNullRequest()
        {
            var repo = new FriendRepository(mockContext.Object);

            var options = new FriendshipCommitOptions 
            {
                Request = null 
            };

            Assert.ThrowsException<ArgumentNullException>(() => repo.CommitFriendshipAcceptance(options));
        }

        [TestMethod]
        public void TestDeleteFriendshipsReturnsFalseIfNoneFound()
        {
            var emptyList = new List<Friendship>();
            var mockFriendshipSet = GetMockDbSet(emptyList);
            mockContext.Setup(c => c.Friendship).Returns(mockFriendshipSet.Object);
            var repo = new FriendRepository(mockContext.Object);
            bool result = repo.DeleteFriendships(FRIEND_ID, USER_ID);
            Assert.IsFalse(result);
        }
    }
}
