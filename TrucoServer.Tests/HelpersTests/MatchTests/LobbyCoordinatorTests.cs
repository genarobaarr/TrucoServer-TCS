using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class LobbyCoordinatorTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private LobbyCoordinator coordinator;

        private const int LOBBY_ID_1 = 1;
        private const int LOBBY_ID_5 = 5;
        private const int LOBBY_ID_10 = 10;
        private const int LOBBY_ID_77 = 77;
        private const int LOBBY_ID_99 = 99;
        
        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            coordinator = new LobbyCoordinator(mockContext.Object);
        }

        [TestMethod]
        public void TestRegisterLobbyMappingStoresLobbyIdCorrectly()
        {
            string matchCode = "CODE12";
            var lobby = new Lobby
            {
                lobbyID = LOBBY_ID_10
            };

            coordinator.RegisterLobbyMapping(matchCode, lobby);
            bool found = coordinator.TryGetLobbyIdFromCode(matchCode, out int retrievedId);
            Assert.AreEqual(10, retrievedId);
        }

        [TestMethod]
        public void TestTryGetLobbyIdFromCodeReturnsTrueIfMappingExists()
        {
            string matchCode = "EXIST1";

            coordinator.RegisterLobbyMapping(matchCode, new Lobby
            {
                lobbyID = LOBBY_ID_5
            });

            bool result = coordinator.TryGetLobbyIdFromCode(matchCode, out _);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRemoveLobbyMappingRemovesTheEntry()
        {
            string matchCode = "REMOVE";

            coordinator.RegisterLobbyMapping(matchCode, new Lobby 
            { 
                lobbyID = LOBBY_ID_1
            });

            coordinator.RemoveLobbyMapping(matchCode);
            bool found = coordinator.TryGetLobbyIdFromCode(matchCode, out _);
            Assert.IsFalse(found);
        }

        [TestMethod]
        public void TestGetPlayerInfoFromCallbackReturnsNullForUnknownCallback()
        {
            var mockCallback = new Mock<ITrucoCallback>();
            var result = coordinator.GetPlayerInfoFromCallback(mockCallback.Object);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetOrCreateLobbyLockReturnsSameObjectForSameId()
        {
            int lobbyId = LOBBY_ID_99;
            var lock1 = coordinator.GetOrCreateLobbyLock(lobbyId);
            var lock2 = coordinator.GetOrCreateLobbyLock(lobbyId);
            Assert.AreSame(lock1, lock2);
        }

        [TestMethod]
        public void TestGetMatchCodeFromLobbyIdReturnsCorrectCode()
        {
            string expectedCode = "FINDME";

            coordinator.RegisterLobbyMapping(expectedCode, new Lobby 
            {
                lobbyID = LOBBY_ID_77
            });

            string actualCode = coordinator.GetMatchCodeFromLobbyId(77);
            Assert.AreEqual(expectedCode, actualCode);
        }

        [TestMethod]
        public void TestGetGuestCountInMemoryReturnsZeroWhenNoGuestsRegistered()
        {
            string matchCode = "EMPTY1";

            coordinator.RegisterLobbyMapping(matchCode, new Lobby
            { 
                lobbyID = LOBBY_ID_1
            });

            int count = coordinator.GetGuestCountInMemory(matchCode);
            Assert.AreEqual(0, count);
        }
    }
}
