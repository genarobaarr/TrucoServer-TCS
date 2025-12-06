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
        private Mock<DbSet<Friendship>> mockFriendshipSet;
        private Mock<DbSet<User>> mockUserSet;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockFriendshipSet = new Mock<DbSet<Friendship>>();
            mockUserSet = new Mock<DbSet<User>>();

            mockContext.Setup(c => c.Friendship).Returns(mockFriendshipSet.Object);
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
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
            var repo = new FriendRepository(mockContext.Object);

            var friendships = new List<Friendship>
            {
                new Friendship 
                {
                    userID = 2,
                    friendID = 1
                } 
            }.AsQueryable();

            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Provider).Returns(friendships.Provider);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Expression).Returns(friendships.Expression);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.ElementType).Returns(friendships.ElementType);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.GetEnumerator()).Returns(friendships.GetEnumerator());
            bool exists = repo.CheckFriendshipExists(1, 2);
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
            var repo = new FriendRepository(mockContext.Object);
            var empty = new List<Friendship>().AsQueryable();

            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Provider).Returns(empty.Provider);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.Expression).Returns(empty.Expression);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.ElementType).Returns(empty.ElementType);
            mockFriendshipSet.As<IQueryable<Friendship>>().Setup(m => m.GetEnumerator()).Returns(empty.GetEnumerator());
            bool result = repo.DeleteFriendships(1, 2);
            Assert.IsFalse(result);
        }
    }
}
