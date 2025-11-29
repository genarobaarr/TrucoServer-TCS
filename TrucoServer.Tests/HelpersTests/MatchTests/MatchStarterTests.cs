using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class MatchStarterTests
    {
        private MatchStarter starter;
        private Mock<IGameRegistry> mockRegistry;
        private Mock<ILobbyCoordinator> mockCoordinator;
        private Mock<ILobbyRepository> mockRepo;
        private Mock<IDeckShuffler> mockShuffler;
        private Mock<IGameManager> mockGameManager;
        private Mock<ITrucoCallback> mockCallback;
        private delegate bool TryGetCallbackDelegate(string username, out ITrucoCallback callback);

        [TestInitialize]
        public void Setup()
        {
            mockRegistry = new Mock<IGameRegistry>();
            mockCoordinator = new Mock<ILobbyCoordinator>();
            mockRepo = new Mock<ILobbyRepository>();
            mockShuffler = new Mock<IDeckShuffler>();
            mockGameManager = new Mock<IGameManager>();
            mockCallback = new Mock<ITrucoCallback>();

            starter = new MatchStarter(
                mockRegistry.Object,
                mockCoordinator.Object,
                mockRepo.Object,
                mockShuffler.Object,
                mockGameManager.Object
            );

            using (var context = new baseDatosTrucoEntities())
            {
                if (!context.User.Any(u => u.username == "StarterUser"))
                {
                    context.User.Add(new User 
                    { 
                        username = "StarterUser",
                        email = "SU@gmail.com",
                        passwordHash = "password", 
                        nameChangeCount = 0
                    });

                    context.SaveChanges();
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var u = context.User.FirstOrDefault(x => x.username == "StarterUser");
               
                if (u != null) 
                { 
                    context.User.Remove(u); context.SaveChanges();
                }
            }
        }

        private void SetupMockForBuilder(string username, out List<PlayerInfo> inputList)
        {
            inputList = new List<PlayerInfo>
            {
                new PlayerInfo
                {
                    Username = username,
                    Team = "Team 1"
                }
            };

            ITrucoCallback dummyCb;
            mockCoordinator.Setup(c => c.TryGetActiveCallbackForPlayer(username, out dummyCb))
                .Returns(new TryGetCallbackDelegate((string user, out ITrucoCallback cb) =>
                {
                    cb = mockCallback.Object;
                    return true;
                }));
        }

        [TestMethod]
        public void TestBuildGamePlayersRegisteredUserShouldReturnTrue()
        {
            SetupMockForBuilder("StarterUser", out var inputList);
            bool result = starter.BuildGamePlayersAndCallbacks(inputList, out _, out _);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestBuildGamePlayersRegisteredUserShouldPopulateList()
        {
            SetupMockForBuilder("StarterUser", out var inputList);
            starter.BuildGamePlayersAndCallbacks(inputList, out var players, out _);
            Assert.AreEqual(1, players.Count);
        }

        [TestMethod]
        public void TestBuildGamePlayersRegisteredUserShouldResolveIdFromDb()
        {
            SetupMockForBuilder("StarterUser", out var inputList);
            starter.BuildGamePlayersAndCallbacks(inputList, out var players, out _);
            Assert.IsTrue(players[0].PlayerID > 0);
        }

        [TestMethod]
        public void TestBuildGamePlayersRegisteredUserShouldPopulateCallbacks()
        {
            SetupMockForBuilder("StarterUser", out var inputList);
            starter.BuildGamePlayersAndCallbacks(inputList, out var players, out var callbacks);
            Assert.IsTrue(callbacks.ContainsKey(players[0].PlayerID));
        }

        [TestMethod]
        public void TestBuildGamePlayersGuestUserShouldReturnTrue()
        {
            SetupMockForBuilder("Guest_123", out var inputList);
            bool result = starter.BuildGamePlayersAndCallbacks(inputList, out _, out _);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestBuildGamePlayersGuestUserShouldGenerateNegativeId()
        {
            SetupMockForBuilder("Guest_123", out var inputList);
            starter.BuildGamePlayersAndCallbacks(inputList, out var players, out _);
            Assert.IsTrue(players[0].PlayerID < 0);
        }

        [TestMethod]
        public void TestInitializeAndRegisterGameShouldAddToRegistry()
        {
            var players = new List<PlayerInformation>();
            var cbs = new Dictionary<int, ITrucoCallback>();

            mockGameManager.Setup(gm => gm.SaveMatchToDatabase(It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<List<PlayerInformation>>())).Returns(1);

            mockRegistry.Setup(r => r.TryAddGame(It.IsAny<string>(), It.IsAny<TrucoMatch>()))
                .Returns(true);

            starter.InitializeAndRegisterGame("MATCH1", 100, players, cbs);
            mockRegistry.Verify(r => r.TryAddGame("MATCH1", It.IsAny<TrucoMatch>()), Times.Once);
        }

        [TestMethod]
        public void TestHandleMatchStartupCleanupShouldCloseLobby()
        {
            string code = "MATCH1";
            int lobbyId = 50;

            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode(code, out lobbyId)).Returns(true);
            starter.HandleMatchStartupCleanup(code);
            mockRepo.Verify(r => r.CloseLobbyById(lobbyId), Times.Once);
        }

        [TestMethod]
        public void TestHandleMatchStartupCleanupShouldExpireInvitations()
        {
            string code = "MATCH1";
            int lobbyId = 50;

            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode(code, out lobbyId)).Returns(true);
            starter.HandleMatchStartupCleanup(code);
            mockRepo.Verify(r => r.ExpireInvitationByMatchCode(code), Times.Once);
        }

        [TestMethod]
        public void TestHandleMatchStartupCleanupShouldRemoveLobbyMapping()
        {
            string code = "MATCH1";
            int lobbyId = 50;

            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode(code, out lobbyId)).Returns(true);
            starter.HandleMatchStartupCleanup(code);
            mockCoordinator.Verify(c => c.RemoveLobbyMapping(code), Times.Once);
        }
    }
}