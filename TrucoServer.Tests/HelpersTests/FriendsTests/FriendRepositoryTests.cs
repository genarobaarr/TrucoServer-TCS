using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Friends;

namespace TrucoServer.Tests.HelpersTests.FriendsTests
{
    [TestClass]
    public class FriendRepositoryTests
    {
        private FriendRepository repository;
        private User userA;
        private User userB;

        [TestInitialize]
        public void Setup()
        {
            repository = new FriendRepository();

            using (var context = new baseDatosTrucoEntities())
            {
                userA = new User
                { 
                    username = "TestUserA", 
                    email = "a@gmail.com", 
                    passwordHash = "hash",
                    nameChangeCount = 0, 
                };

                userB = new User 
                { 
                    username = "TestUserB", 
                    email = "b@gmail.com", 
                    passwordHash = "hash", 
                    nameChangeCount = 0,
                };

                context.User.Add(userA);
                context.User.Add(userB);
                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var friendships = context.Friendship.Where(f => f.userID == userA.userID || f.userID == userB.userID);
                context.Friendship.RemoveRange(friendships);
                var users = context.User.Where(u => u.username == "TestUserA" || u.username == "TestUserB");
                context.User.RemoveRange(users);

                context.SaveChanges();
            }
        }

        [TestMethod]
        public void TestRegisterFriendRequestValidIdsShouldInsertPendingRow()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                repository.RegisterFriendRequest(context, userA.userID, userB.userID, "Pending");
                var request = context.Friendship.FirstOrDefault(f => f.userID == userA.userID && f.friendID == userB.userID);
                Assert.IsNotNull(request, "Friendship row should exist in DB");
                Assert.AreEqual("Pending", request.status);
            }
        }

        [TestMethod]
        public void TestCheckFriendshipExistsExistingRelationShouldReturnTrue()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var f = new Friendship 
                { 
                    userID = userA.userID,
                    friendID = userB.userID, 
                    status = "Accepted" 
                };

                context.Friendship.Add(f);
                context.SaveChanges();
                bool exists = repository.CheckFriendshipExists(context, userA.userID, userB.userID);
                Assert.IsTrue(exists);
            }
        }

        [TestMethod]
        public void TestCommitFriendshipAcceptancePendingRequestShouldCreateReciprocalRows()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var request = new Friendship 
                {
                    userID = userA.userID,
                    friendID = userB.userID, 
                    status = "Pending" 
                };

                context.Friendship.Add(request);
                context.SaveChanges();

                repository.CommitFriendshipAcceptance(context, request, userA.userID, userB.userID, "Accepted");
                Assert.AreEqual("Accepted", request.status);
                var reciprocal = context.Friendship.FirstOrDefault(f => f.userID == userB.userID && f.friendID == userA.userID);
                Assert.IsNotNull(reciprocal, "Reciprocal friendship should be created");
                Assert.AreEqual("Accepted", reciprocal.status);
            }
        }

        [TestMethod]
        public void TestDeleteFriendshipsExistingRelationShouldRemoveBothRows()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                context.Friendship.Add(new Friendship
                { 
                    userID = userA.userID, 
                    friendID = userB.userID, 
                    status = "Accepted" 
                });

                context.Friendship.Add(new Friendship 
                { 
                    userID = userB.userID, 
                    friendID = userA.userID, 
                    status = "Accepted"
                });

                context.SaveChanges();
                bool result = repository.DeleteFriendships(context, userA.userID, userB.userID);

                int count = context.Friendship.Count(f => f.userID == userA.userID || f.userID == userB.userID);
                Assert.AreEqual(0, count, "All rows between A and B should be deleted");
            }
        }

        [TestMethod]
        public void TestQueryFriendsListAcceptedStatusShouldReturnFriendDataList()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                context.UserProfile.Add(new UserProfile 
                { 
                    userID = userB.userID, 
                    avatarID = "avatar_aaa_default", 
                    languageCode = "es-MX", 
                    isMusicMuted = false 
                });

                context.Friendship.Add(new Friendship 
                { 
                    userID = userA.userID, 
                    friendID = userB.userID, 
                    status = "Accepted" 
                });

                context.SaveChanges();
                var friends = repository.QueryFriendsList(context, userA.userID, "Accepted");
                Assert.AreEqual(1, friends.Count);
                Assert.AreEqual("TestUserB", friends[0].Username);
            }
        }
    }
}
