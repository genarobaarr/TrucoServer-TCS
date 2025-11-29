using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class JoinServiceTests
    {
        private JoinService service;
        private Mock<ILobbyCoordinator> mockCoordinator;
        private User testUser;
        private Lobby publicLobby;

        [TestInitialize]
        public void Setup()
        {
            mockCoordinator = new Mock<ILobbyCoordinator>();
            service = new JoinService(mockCoordinator.Object);

            using (var context = new baseDatosTrucoEntities())
            {
                testUser = new User
                { 
                    username = "JoinUser",
                    email = "JU@gmail.com", 
                    passwordHash = "password", 
                    nameChangeCount = 0, 
                };

                context.User.Add(testUser);

                publicLobby = new Lobby 
                { 
                    ownerID = 999,
                    versionID = 1,
                    maxPlayers = 2, 
                    status = "Public", 
                    createdAt = DateTime.Now
                };

                context.Lobby.Add(publicLobby);
                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == "JoinUser");

                if (user != null)
                {
                    context.User.Remove(user);
                }
                
                var lobby = context.Lobby.FirstOrDefault(l => l.ownerID == 999);
                
                if (lobby != null)
                {
                    context.LobbyMember.RemoveRange(context.LobbyMember.Where(m => m.lobbyID == lobby.lobbyID));
                    context.Lobby.Remove(lobby);
                }

                context.SaveChanges();
            }
        }

        [TestMethod]
        public void TestProcessSafeJoinPublicLobbyAsUserShouldAddMember()
        {
            mockCoordinator.Setup(c => c.GetGuestCountInMemory(It.IsAny<string>())).Returns(0);
            bool result = service.ProcessSafeJoin(publicLobby.lobbyID, "MATCHCODE", "JoinUser");
            Assert.IsTrue(result);

            using (var context = new baseDatosTrucoEntities())
            {
                bool isMember = context.LobbyMember.Any(m => m.lobbyID == publicLobby.lobbyID && m.userID == testUser.userID);
                Assert.IsTrue(isMember);
            }
        }

        [TestMethod]
        public void TestProcessSafeJoinAsGuestShouldSucceedIfRoom()
        {
            mockCoordinator.Setup(c => c.GetGuestCountInMemory(It.IsAny<string>())).Returns(0);
            bool result = service.ProcessSafeJoin(publicLobby.lobbyID, "MATCHCODE", "Guest_User");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestProcessSafeJoinLobbyFullShouldFail()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var u2 = new User 
                { 
                    username = "TestUser", 
                    email = "TU@gmail.com",
                    passwordHash = "password", 
                    nameChangeCount = 0 
                };

                context.User.Add(u2);
                context.SaveChanges();
                context.LobbyMember.Add(new LobbyMember 
                {
                    lobbyID = publicLobby.lobbyID,
                    userID = u2.userID,
                    role = "Player", 
                    team = "Team 1" 
                });

                var u3 = new User 
                { 
                    username = "TestUser2", 
                    email = "TU2@gmail.com", 
                    passwordHash = "password", 
                    nameChangeCount = 0
                };

                context.User.Add(u3);
                context.SaveChanges();
               
                context.LobbyMember.Add(new LobbyMember
                { 
                    lobbyID = publicLobby.lobbyID, 
                    userID = u3.userID,
                    role = "Player", 
                    team = "Team 2" 
                });

                context.SaveChanges();
            }

            mockCoordinator.Setup(c => c.GetGuestCountInMemory(It.IsAny<string>())).Returns(0);
            bool result = service.ProcessSafeJoin(publicLobby.lobbyID, "MATCHCODE", "JoinUser");
            Assert.IsFalse(result, "Should fail because lobby is full (2/2)");
        }
    }
}
