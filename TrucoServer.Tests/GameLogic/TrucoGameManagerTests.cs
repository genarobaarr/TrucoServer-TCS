using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests.GameLogic
{
    [TestClass]
    public class TrucoGameManagerTests
    {
        private TrucoGameManager manager;
        private List<User> testUsers;
        private int testLobbyId;
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";

        [TestInitialize]
        public void Setup()
        {
            manager = new TrucoGameManager();

            using (var context = new baseDatosTrucoEntities())
            {
                testUsers = new List<User>
            {
                new User
                {
                    username = "TestUser1",
                    email = "TU1@gmail.com",
                    passwordHash = "password",
                    nameChangeCount = 0,
                    wins = 0,
                    losses = 0
                },

                new User
                {
                    username = "TestUser2",
                    email = "TU2@gmail.com",
                    passwordHash = "password",
                    nameChangeCount = 0,
                    wins = 0,
                    losses = 0
                }
            };

                context.User.AddRange(testUsers);
                context.SaveChanges();

                var lobby = new Lobby
                {
                    ownerID = testUsers[0].userID,
                    versionID = 1,
                    maxPlayers = 2,
                    status = "Public",
                    createdAt = DateTime.Now
                };

                context.Lobby.Add(lobby);
                context.SaveChanges();
                testLobbyId = lobby.lobbyID;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var users = context.User.Where(u => u.username.StartsWith("TestUser")).ToList();
                if (users.Any())
                {
                    var userIds = users.Select(u => u.userID).ToList();

                    var matchPlayers = context.MatchPlayer.Where(mp => userIds.Contains(mp.userID));
                    context.MatchPlayer.RemoveRange(matchPlayers);

                    var matches = context.Match.Where(m => m.lobbyID == testLobbyId);
                    context.Match.RemoveRange(matches);

                    var members = context.LobbyMember.Where(lm => lm.lobbyID == testLobbyId);
                    context.LobbyMember.RemoveRange(members);

                    var lobby = context.Lobby.Find(testLobbyId);
                    if (lobby != null) context.Lobby.Remove(lobby);

                    context.User.RemoveRange(users);

                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseNewMatchShouldReturnPositiveId()
        {
            var players = new List<PlayerInformation>();
            int matchId = manager.SaveMatchToDatabase("MATCH001", testLobbyId, players);
            Assert.IsTrue(matchId > 0);
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseExistingInProgressMatchShouldReturnSameId()
        {
            int firstId = manager.SaveMatchToDatabase("MATCH001", testLobbyId, new List<PlayerInformation>());
            int secondId = manager.SaveMatchToDatabase("MATCH001", testLobbyId, new List<PlayerInformation>());
            Assert.AreEqual(firstId, secondId);
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseRegisteredPlayersShouldBeAddedToDb()
        {
            var players = new List<PlayerInformation>
        {
            new PlayerInformation(testUsers[0].userID, testUsers[0].username, TEAM_1),
            new PlayerInformation(testUsers[1].userID, testUsers[1].username, TEAM_2)
        };

            int matchId = manager.SaveMatchToDatabase("MATCH002", testLobbyId, players);

            using (var context = new baseDatosTrucoEntities())
            {
                int count = context.MatchPlayer.Count(mp => mp.matchID == matchId);
                Assert.AreEqual(2, count);
            }
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseGuestPlayersShouldBeSkipped()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(testUsers[0].userID, testUsers[0].username, TEAM_1),
                new PlayerInformation(-12345, "Guest_1", TEAM_2)
            };

            int matchId = manager.SaveMatchToDatabase("MATCH003", testLobbyId, players);

            using (var context = new baseDatosTrucoEntities())
            {
                int count = context.MatchPlayer.Count(mp => mp.matchID == matchId);
                Assert.AreEqual(1, count);
            }
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseFourPlayersShouldSetVersionTwo()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(1, "A", TEAM_1),
                new PlayerInformation(2, "B", TEAM_1),
                new PlayerInformation(3, "C", TEAM_2),
                new PlayerInformation(4, "D", TEAM_2)
            };

            int matchId = manager.SaveMatchToDatabase("MATCH004", testLobbyId, players);

            using (var context = new baseDatosTrucoEntities())
            {
                var match = context.Match.Find(matchId);
                Assert.AreEqual(2, match.versionID);
            }
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseInvalidLobbyIdShouldReturnMinusOne()
        {
            int invalidLobbyId = -999;
            int result = manager.SaveMatchToDatabase("MATCH_FAIL", invalidLobbyId, new List<PlayerInformation>());
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestSaveMatchResultValidMatchShouldUpdateStatusToFinished()
        {
            int matchId = manager.SaveMatchToDatabase("MATCH_END", testLobbyId, new List<PlayerInformation>());
            manager.SaveMatchResult(matchId, TEAM_1, 30, 15);

            using (var context = new baseDatosTrucoEntities())
            {
                var match = context.Match.Find(matchId);
                Assert.AreEqual("Finished", match.status);
            }
        }

        [TestMethod]
        public void TestSaveMatchResultValidMatchShouldUpdateUserStats()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(testUsers[0].userID, testUsers[0].username, TEAM_1),
                new PlayerInformation(testUsers[1].userID, testUsers[1].username, TEAM_2)
            };
           
            int matchId = manager.SaveMatchToDatabase("MATCH_STATS", testLobbyId, players);
            manager.SaveMatchResult(matchId, TEAM_1, 30, 0);

            using (var context = new baseDatosTrucoEntities())
            {
                var winner = context.User.Find(testUsers[0].userID);
                var loser = context.User.Find(testUsers[1].userID);
                bool statsUpdated = winner.wins == 1 && loser.losses == 1;
                Assert.IsTrue(statsUpdated);
            }
        }

        [TestMethod]
        public void TestSaveMatchResultNonExistentMatchShouldHandleGracefully()
        {
            try
            {
                manager.SaveMatchResult(-500, TEAM_1, 30, 0);
            }
            catch
            {
                Assert.Fail("Should not throw exception for missing match");
            }
        }

        [TestMethod]
        public void TestSaveMatchResultShouldSetEndedAtTimestamp()
        {
            int matchId = manager.SaveMatchToDatabase("MATCH_TIME", testLobbyId, new List<PlayerInformation>());
            manager.SaveMatchResult(matchId, TEAM_1, 30, 15);

            using (var context = new baseDatosTrucoEntities())
            {
                var match = context.Match.Find(matchId);
                Assert.IsNotNull(match.endedAt);
            }
        }
    }
}
