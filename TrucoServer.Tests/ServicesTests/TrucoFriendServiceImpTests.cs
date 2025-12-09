using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Friends;
using TrucoServer.Services;

namespace TrucoServer.Tests.ServicesTests
{
    [TestClass]
    public class TrucoFriendServiceImpTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<IFriendRepository> mockRepo;
        private Mock<IFriendNotifier> mockNotifier;
        private TrucoFriendServiceImp service;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockRepo = new Mock<IFriendRepository>();
            mockNotifier = new Mock<IFriendNotifier>();
            var emptyUsers = GetMockDbSet(new List<User>());
            mockContext.Setup(c => c.User).Returns(emptyUsers.Object);

            service = new TrucoFriendServiceImp(mockContext.Object, mockRepo.Object, mockNotifier.Object);
        }

        private static Mock<DbSet<T>> GetMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return mockSet;
        }

        [TestMethod]
        public void TestSendFriendRequestReturnsFalseIfUsersAreSame()
        {
            bool result = service.SendFriendRequest("User1", "User1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSendFriendRequestReturnsFalseIfLookupFails()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                 .Returns(new UserLookupResult
                 {
                     Success = false
                 });

            Assert.ThrowsException<FaultException<CustomFault>>(() => service.SendFriendRequest("UserA", "UserB"));
        }

        [TestMethod]
        public void TestSendFriendRequestThrowsFaultExceptionIfFriendshipExists()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult { Success = true, User1 = new User 
                    {
                        userID = 1 
                    }, 
                        User2 = new User
                        {
                            userID = 2 
                        }
                    });

            mockRepo.Setup(r => r.CheckFriendshipExists(1, 2)).Returns(true);
            Assert.ThrowsException<FaultException<CustomFault>>(() => service.SendFriendRequest("UserA", "UserB"));
        }

        [TestMethod]
        public void TestSendFriendRequestReturnsTrueOnSuccess()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    {
                        Success = true, 
                        User1 = new User 
                        {
                            userID = 1 
                        },
                        User2 = new User 
                        {
                            userID = 2 
                        } 
                    });

            mockRepo.Setup(r => r.CheckFriendshipExists(1, 2)).Returns(false);
            bool result = service.SendFriendRequest("UserA", "UserB");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSendFriendRequestThrowsFaultExceptionOnDbError()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Throws(new Exception("DB Error"));
            Assert.ThrowsException<FaultException<CustomFault>>(() => service.SendFriendRequest("UserA", "UserB"));
        }

        [TestMethod]
        public void TestAcceptFriendRequestReturnsFalseIfNoPendingRequest()
        {
            // Arrange
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    { 
                        Success = true, 
                        User1 = new User 
                        {
                            userID = 1 
                        },

                        User2 = new User 
                        {
                            userID = 2 
                        }
                    });

            mockRepo.Setup(r => r.FindPendingFriendship(It.IsAny<FriendRequest>())).Returns((Friendship)null);
            bool result = service.AcceptFriendRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestReturnsTrueOnSuccess()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    { 
                        Success = true,
                        User1 = new User 
                        {
                            userID = 1 
                        },

                        User2 = new User 
                        { 
                            userID = 2 
                        } 
                    });

            mockRepo.Setup(r => r.FindPendingFriendship(It.IsAny<FriendRequest>())).Returns(new Friendship());

            bool result = service.AcceptFriendRequest("UserA", "UserB");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRemoveFriendOrRequestReturnsFalseIfLookupFails()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    {
                        Success = false
                    });

            bool result = service.RemoveFriendOrRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRemoveFriendOrRequestReturnsTrueOnDelete()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult
                    { 
                        Success = true,
                        User1 = new User 
                        { 
                            userID = 1 
                        },
                        User2 = new User
                        { 
                            userID = 2 
                        } 
                    });

            mockRepo.Setup(r => r.DeleteFriendships(1, 2)).Returns(true);

            bool result = service.RemoveFriendOrRequest("UserA", "UserB");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestGetFriendsReturnsEmptyListWhenUserNotFound()
        {
            var emptySet = GetMockDbSet(new List<User>());
            mockContext.Setup(c => c.User).Returns(emptySet.Object);
            var result = service.GetFriends("NonExistentUser");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetFriendsThrowsFaultExceptionOnDbException()
        {
            var mockSet = new Mock<DbSet<User>>();
            mockSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("DB"));
            mockContext.Setup(c => c.User).Returns(mockSet.Object);
            Assert.ThrowsException<FaultException<CustomFault>>(() => service.GetFriends("UserA"));
        }

        [TestMethod]
        public void TestGetPendingFriendRequestsReturnsEmptyWhenUserNotFound()
        {
            var emptySet = GetMockDbSet(new List<User>());
            mockContext.Setup(c => c.User).Returns(emptySet.Object);
            var result = service.GetPendingFriendRequests("NonExistentUser");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetPendingFriendRequestsThrowsFaultExceptionOnDbException()
        {
            var mockSet = new Mock<DbSet<User>>();
            mockSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("DB"));
            mockContext.Setup(c => c.User).Returns(mockSet.Object);
            Assert.ThrowsException<FaultException<CustomFault>>(() => service.GetPendingFriendRequests("UserA"));
        }
    }
}