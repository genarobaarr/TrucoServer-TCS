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

        [TestMethod]
        public void TestBuildGamePlayersRegisteredUserShouldResolveIdFromDb()
        {
            var inputList = new List<PlayerInfo>
        {
            new PlayerInfo 
            { 
                Username = "StarterUser", 
                Team = "Team 1"
            }
        };

            ITrucoCallback dummyCb;
            mockCoordinator.Setup(c => c.TryGetActiveCallbackForPlayer("StarterUser", out dummyCb))
                .Returns(new TryGetCallbackDelegate((string user, out ITrucoCallback cb) => 
                {
                    cb = mockCallback.Object;
                    return true;
                }));

            bool result = starter.BuildGamePlayersAndCallbacks(inputList, out var players, out var callbacks);
            Assert.IsTrue(result);
            Assert.AreEqual(1, players.Count);
            Assert.IsTrue(players[0].PlayerID > 0);
            Assert.IsTrue(callbacks.ContainsKey(players[0].PlayerID));
        }

        [TestMethod]
        public void TestBuildGamePlayersGuestUserShouldGenerateNegativeId()
        {
            var inputList = new List<PlayerInfo>
        {
            new PlayerInfo 
            { 
                Username = "Guest_123", 
                Team = "Team 2" 
            }
        };

            ITrucoCallback dummyCb;
            mockCoordinator.Setup(c => c.TryGetActiveCallbackForPlayer("Guest_123", out dummyCb))
                .Returns(new TryGetCallbackDelegate((string user, out ITrucoCallback cb) => 
                {
                    cb = mockCallback.Object;
                    return true;
                }));

            bool result = starter.BuildGamePlayersAndCallbacks(inputList, out var players, out var callbacks);
            Assert.IsTrue(result);
            Assert.IsTrue(players[0].PlayerID < 0);
        }

        [TestMethod]
        public void TestInitializeAndRegisterGameShouldCreateMatchAndAddToRegistry()
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
        public void TestHandleMatchStartupCleanupShouldCloseLobbyAndCleanMappings()
        {
            string code = "MATCH1";
            int lobbyId = 50;

            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode(code, out lobbyId)).Returns(true);
            starter.HandleMatchStartupCleanup(code);
            mockRepo.Verify(r => r.CloseLobbyById(lobbyId), Times.Once);
            mockRepo.Verify(r => r.ExpireInvitationByMatchCode(code), Times.Once);
            mockCoordinator.Verify(c => c.RemoveLobbyMapping(code), Times.Once);
        }
    }
}