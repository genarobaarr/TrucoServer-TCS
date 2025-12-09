using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
        private Mock<DbSet<User>> mockUserSet;

        private TrucoFriendServiceImp service;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockRepo = new Mock<IFriendRepository>();
            mockNotifier = new Mock<IFriendNotifier>();
            mockUserSet = new Mock<DbSet<User>>();
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            service = new TrucoFriendServiceImp(mockContext.Object, mockRepo.Object, mockNotifier.Object);
        }


        [TestMethod]
        public void TestSendFriendRequestReturnsFalseIfUsersAreSame()
        {
            bool result = service.SendFriendRequest("UserA", "UserA");
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

            bool result = service.SendFriendRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSendFriendRequestReturnsFalseIfFriendshipExists()
        {
            var user1 = new User
            { 
                userID = 1 
            };

            var user2 = new User 
            { 
                userID = 2
            };

            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    { 
                        Success = true,
                        User1 = user1,
                        User2 = user2 
                    });

            mockRepo.Setup(r => r.CheckFriendshipExists(1, 2)).Returns(true);
            bool result = service.SendFriendRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSendFriendRequestReturnsTrueOnSuccess()
        {
            var user1 = new User 
            { 
                userID = 1
            };

            var user2 = new User 
            {
                userID = 2
            };

            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    {
                        Success = true,
                        User1 = user1,
                        User2 = user2 
                    });

            mockRepo.Setup(r => r.CheckFriendshipExists(1, 2)).Returns(false);
            bool result = service.SendFriendRequest("UserA", "UserB");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSendFriendRequestHandlesException()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Throws(new Exception("DB Error"));

            bool result = service.SendFriendRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void TestAcceptFriendRequestReturnsFalseIfNoPendingRequest()
        {
            var user1 = new User
            { 
                userID = 1 
            };
           
            var user2 = new User
            { 
                userID = 2 
            };

            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult
                    { 
                        Success = true, 
                        User1 = user1, 
                        User2 = user2 
                    });
           
            mockRepo.Setup(r => r.FindPendingFriendship(It.IsAny<FriendRequest>())).Returns((Friendship)null);
            bool result = service.AcceptFriendRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestReturnsTrueOnSuccess()
        {
            var user1 = new User
            { 
                userID = 1
            };
           
            var user2 = new User 
            { 
                userID = 2 
            };

            var friendship = new Friendship();

            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult
                    { 
                        Success = true, 
                        User1 = user1,
                        User2 = user2
                    });

            mockRepo.Setup(r => r.FindPendingFriendship(It.IsAny<FriendRequest>())).Returns(friendship);
            bool result = service.AcceptFriendRequest("UserA", "UserB");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestAcceptFriendRequestValidatesInputs()
        {
            bool result = service.AcceptFriendRequest(string.Empty, "UserB");
            Assert.IsFalse(result);
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
            var user1 = new User 
            {
                userID = 1 
            };
          
            var user2 = new User 
            { 
                userID = 2
            };

            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>()))
                    .Returns(new UserLookupResult 
                    {
                        Success = true, 
                        User1 = user1,
                        User2 = user2 
                    });

            mockRepo.Setup(r => r.DeleteFriendships(1, 2)).Returns(true);
            bool result = service.RemoveFriendOrRequest("UserA", "UserB");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRemoveFriendOrRequestHandlesException()
        {
            mockRepo.Setup(r => r.GetUsersFromDatabase(It.IsAny<UserLookupOptions>())).Throws(new Exception("DB"));
            bool result = service.RemoveFriendOrRequest("UserA", "UserB");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestGetFriendsReturnsEmptyIfUserNotFound()
        {
            var data = new List<User>().AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            var result = service.GetFriends("Test");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetFriendsHandlesException()
        {
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("DB"));
            var result = service.GetFriends("UserA");
            Assert.AreEqual(0, result.Count);
        }


        [TestMethod]
        public void TestGetPendingFriendRequestsReturnsEmptyIfUserNotFound()
        {
            var data = new List<User>().AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            var result = service.GetPendingFriendRequests("Test");
            Assert.AreEqual(0, result.Count);
        }


        [TestMethod]
        public void TestGetPendingFriendRequestsHandlesException()
        {
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("DB"));
            var result = service.GetPendingFriendRequests("UserA");
            Assert.AreEqual(0, result.Count);
        }
    }
}