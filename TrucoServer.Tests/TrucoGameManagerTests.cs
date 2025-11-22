using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests
{
    [TestClass]
    public class TrucoGameManagerTests
    {
        private TrucoGameManager gameManager;
        private int testLobbyId;
        private int testUserAId;
        private int testUserBId;
        private const string TEST_USER_A = "User A";
        private const string TEST_USER_B = "User B";
        private const string TEST_PASSWORD_HASH = "password";
        private const string TEST_EMAIL_ADDRESS = "testa@email.com";
        private const string TEST_LOBBY_STATUS = "Active";
        private const string TEST_TEAM_1 = "Team 1";
        private const string TEST_TEAM_2 = "Team 2";
        private const string TEST_LOBBY_IN_PROGRESS_STATUS = "InProgress";
        private const string TEST_LOBBY_FINISHED_STATUS = "Finished";
        private const string TEST_LOBBY_CODE = "TESTCODE";
        private const int TEST_WINNER_SCORE = 30;
        private const int TEST_LOSSER_SCORE = 15;
        private const int TEST_USER_WINS = 0;
        private const int TEST_USER_LOSSES = 0;
        private const int TEST_LOBBY_VERSION_1 = 1;
        private const int TEST_LOBBBY_ID_NOT_FOUND = -999;
        private const int TEST_EMPTY_STREAM_LENGTH = 0;

        [TestInitialize]
        public void Setup()
        {
            gameManager = new TrucoGameManager();
            InsertTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            DeleteTestData();
        }

        private void InsertTestData()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var userA = new User
                {
                    username = TEST_USER_A,
                    passwordHash = TEST_PASSWORD_HASH,
                    email = TEST_EMAIL_ADDRESS,
                    wins = TEST_USER_WINS,
                    losses = TEST_USER_LOSSES
                };
                var userB = new User
                {
                    username = TEST_USER_B,
                    passwordHash = TEST_PASSWORD_HASH,
                    email = TEST_EMAIL_ADDRESS,
                    wins = TEST_USER_WINS,
                    losses = TEST_USER_LOSSES
                };

                context.User.Add(userA);
                context.User.Add(userB);
                context.SaveChanges();

                testUserAId = userA.userID;
                testUserBId = userB.userID;

                var lobby = new Lobby
                {
                    ownerID = testUserAId,
                    status = TEST_LOBBY_STATUS,
                    versionID = TEST_LOBBY_VERSION_1,
                    createdAt = DateTime.Now
                };

                context.Lobby.Add(lobby);
                context.SaveChanges();

                testLobbyId = lobby.lobbyID;
            }
        }

        private void DeleteTestData()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var matchPlayers = context.MatchPlayer.Where(mp => mp.Match.lobbyID == testLobbyId);
                context.MatchPlayer.RemoveRange(matchPlayers);

                var matches = context.Match.Where(m => m.lobbyID == testLobbyId);
                context.Match.RemoveRange(matches);

                var lobby = context.Lobby.Find(testLobbyId);
                if (lobby != null)
                {
                    context.Lobby.Remove(lobby);
                }

                var userA = context.User.Find(testUserAId);
                if (userA != null)
                {
                    context.User.Remove(userA);
                }

                var userB = context.User.Find(testUserBId);
                if (userB != null)
                {
                    context.User.Remove(userB);
                }

                context.SaveChanges();
            }
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseReturnsPositiveIdOnSuccess()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(testUserAId, TEST_USER_A, TEST_TEAM_1),
                new PlayerInformation(testUserBId, TEST_USER_B, TEST_TEAM_2)
            };

            int matchId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);

            Assert.IsTrue(matchId > TEST_EMPTY_STREAM_LENGTH);
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseCreatesMatchWithInProgressStatus()
        {
            var players = new List<PlayerInformation>();

            int matchId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);

            using (var context = new baseDatosTrucoEntities())
            {
                var match = context.Match.Find(matchId);
                Assert.AreEqual(TEST_LOBBY_IN_PROGRESS_STATUS, match.status);
            }
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseReturnsSameIdIfMatchInProgressExists()
        {
            var players = new List<PlayerInformation>();
            int firstId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);
            int secondId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);

            Assert.AreEqual(firstId, secondId);
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseReturnsMinusOneOnInvalidLobbyId()
        {
            int invalidLobbyId = TEST_LOBBBY_ID_NOT_FOUND;
            var players = new List<PlayerInformation>();

            int result = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, invalidLobbyId, players);

            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void TestSaveMatchResultUpdatesStatusToFinished()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(testUserAId, TEST_USER_A, TEST_TEAM_1),
                new PlayerInformation(testUserBId, TEST_USER_B, TEST_TEAM_2)
            };
            int matchId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);
            gameManager.SaveMatchResult(matchId, TEST_TEAM_1, TEST_WINNER_SCORE, TEST_LOSSER_SCORE);

            using (var context = new baseDatosTrucoEntities())
            {
                var match = context.Match.Find(matchId);
                Assert.AreEqual(TEST_LOBBY_FINISHED_STATUS, match.status);
            }
        }

        [TestMethod]
        public void TestSaveMatchResultIncrementsWinnerWinsCount()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(testUserAId, TEST_USER_A, TEST_TEAM_1), 
                new PlayerInformation(testUserBId, TEST_USER_B, TEST_TEAM_2)
            };
            int matchId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);
            gameManager.SaveMatchResult(matchId, TEST_TEAM_1, TEST_WINNER_SCORE, TEST_LOSSER_SCORE);

            using (var context = new baseDatosTrucoEntities())
            {
                var winner = context.User.Find(testUserAId);
                Assert.AreEqual(1, winner.wins);
            }
        }

        [TestMethod]
        public void TestSaveMatchResultIncrementsLoserLossesCount()
        {
            var players = new List<PlayerInformation>
            {
                new PlayerInformation(testUserAId, TEST_USER_A, TEST_TEAM_1),
                new PlayerInformation(testUserBId, TEST_USER_B, TEST_TEAM_2) 
            };
            int matchId = gameManager.SaveMatchToDatabase(TEST_LOBBY_CODE, testLobbyId, players);
            gameManager.SaveMatchResult(matchId, TEST_TEAM_1, TEST_WINNER_SCORE, TEST_LOSSER_SCORE);

            using (var context = new baseDatosTrucoEntities())
            {
                var loser = context.User.Find(testUserBId);
                Assert.AreEqual(1, loser.losses);
            }
        }

        [TestMethod]
        public void TestSaveMatchResultHandlesNonExistentMatchGracefully()
        {
            int nonExistentMatchId = TEST_LOBBBY_ID_NOT_FOUND;

            try
            {
                gameManager.SaveMatchResult(nonExistentMatchId, TEST_TEAM_1, TEST_WINNER_SCORE, 0);
            }
            catch (Exception)
            {
                Assert.Fail("Should catch exception internally and log it.");
            }
            Assert.IsTrue(true);
        }
    }
}
