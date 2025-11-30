using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Services;

namespace TrucoServer.Tests.ServicesTests
{
    [TestClass]
    public class TrucoFriendServiceImpTests
    {
        private TrucoMatchServiceImp service;
        private const string HOST_USER = "MatchHost";
        private const string JOIN_USER = "MatchJoiner";

        [TestInitialize]
        public void Setup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                CleanupData(context);

                var host = new User 
                {
                    username = HOST_USER,
                    email = "Host@gmail.com", 
                    passwordHash = "password", 
                    nameChangeCount = 0, 
                };

                var joiner = new User 
                {
                    username = JOIN_USER, 
                    email = "Join@gmail.com",
                    passwordHash = "password", 
                    nameChangeCount = 0, 
                };

                context.User.Add(host);
                context.User.Add(joiner);
                context.SaveChanges();
            }

            service = new TrucoMatchServiceImp();
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                CleanupData(context);
            }
        }

        private void CleanupData(baseDatosTrucoEntities context)
        {
            var users = context.User.Where(u => u.username == HOST_USER || u.username == JOIN_USER).ToList();
          
            if (users.Any())
            {
                var userIds = users.Select(u => u.userID).ToList();
                var members = context.LobbyMember.Where(lm => userIds.Contains(lm.userID));
                context.LobbyMember.RemoveRange(members);
                var lobbies = context.Lobby.Where(l => userIds.Contains(l.ownerID));
              
                foreach (var l in lobbies)
                {
                    var lm = context.LobbyMember.Where(x => x.lobbyID == l.lobbyID);
                    context.LobbyMember.RemoveRange(lm);
                    var inv = context.Invitation.Where(i => i.senderID == l.ownerID);
                    context.Invitation.RemoveRange(inv);
                }

                context.Lobby.RemoveRange(lobbies);
                context.User.RemoveRange(users);
                context.SaveChanges();
            }
        }

        [TestMethod]
        public void TestCreateLobbyValidReturnsCodeLength()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            Assert.AreEqual(6, code.Length);
        }

        [TestMethod]
        public void TestCreateLobbyPersistence()
        {
            service.CreateLobby(HOST_USER, 2, "Public");

            using (var context = new baseDatosTrucoEntities())
            {
                Assert.IsNotNull(context.Lobby.FirstOrDefault(l => l.User.username == HOST_USER));
            }
        }

        [TestMethod]
        public void TestCreateLobbyInvalidUserReturnsEmpty()
        {
            string code = service.CreateLobby("NonExistent", 2, "Public");
            Assert.AreEqual(string.Empty, code);
        }

        [TestMethod]
        public void TestJoinMatchValidReturnsTrue()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            bool result = service.JoinMatch(code, JOIN_USER);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestJoinMatchPersistence()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            service.JoinMatch(code, JOIN_USER);

            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = context.Lobby.FirstOrDefault(l => l.User.username == HOST_USER);
                Assert.IsTrue(context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID && lm.User.username == JOIN_USER));
            }
        }

        [TestMethod]
        public void TestJoinMatchInvalidCodeReturnsFalse()
        {
            bool result = service.JoinMatch("INVALID", JOIN_USER);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestGetPublicLobbiesContainsCreated()
        {
            service.CreateLobby(HOST_USER, 2, "Public");
            var lobbies = service.GetPublicLobbies();
            Assert.IsTrue(lobbies.Any(l => l.MatchName.Contains(HOST_USER)));
        }

        [TestMethod]
        public void TestGetPublicLobbiesExcludesPrivate()
        {
            service.CreateLobby(HOST_USER, 2, "Private");
            var lobbies = service.GetPublicLobbies();
            Assert.IsFalse(lobbies.Any(l => l.MatchName.Contains(HOST_USER)));
        }

        [TestMethod]
        public void TestLeaveMatchRemovesFromDb()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            service.JoinMatch(code, JOIN_USER);
            service.LeaveMatch(code, JOIN_USER);

            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = context.Lobby.FirstOrDefault(l => l.User.username == HOST_USER);
                Assert.IsFalse(context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID && lm.User.username == JOIN_USER));
            }
        }

        [TestMethod]
        public void TestGetLobbyPlayersCount()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            service.JoinMatch(code, JOIN_USER);
            var players = service.GetLobbyPlayers(code);

            Assert.AreEqual(2, players.Count);
        }

        [TestMethod]
        public void TestJoinMatchGuestReturnsTrue()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            bool result = service.JoinMatch(code, "Guest_123");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSwitchTeamUpdatesDb()
        {
            string code = service.CreateLobby(HOST_USER, 2, "Public");
            service.JoinMatch(code, JOIN_USER);
            service.SwitchTeam(code, JOIN_USER);

            using (var context = new baseDatosTrucoEntities())
            {
                var member = context.LobbyMember.FirstOrDefault(lm => lm.User.username == JOIN_USER);
                Assert.IsNotNull(member);
            }
        }

        [TestMethod]
        public void TestGetBannedWordsReturnsNotNull()
        {
            var list = service.GetBannedWords();
            Assert.IsNotNull(list.BannedWords);
        }

        [TestMethod]
        public void TestStartMatchClosesLobbyInDb()
        {
            string code = service.CreateLobby(HOST_USER, 1, "Public");
            service.JoinMatch(code, JOIN_USER);
            service.StartMatch(code);

            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = context.Lobby.FirstOrDefault(l => l.User.username == HOST_USER);
                Assert.AreEqual("Closed", lobby.status);
            }
        }
    }
}