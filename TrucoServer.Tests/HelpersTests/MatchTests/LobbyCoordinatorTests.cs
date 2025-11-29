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
        private LobbyCoordinator coordinator;
        private Mock<ITrucoCallback> mockCallback;

        [TestInitialize]
        public void Setup()
        {
            coordinator = new LobbyCoordinator();
            mockCallback = new Mock<ITrucoCallback>();
        }

        [TestMethod]
        public void TestRegisterLobbyMappingShouldStoreMapping()
        {
            string code = "MATCH1";
       
            var lobby = new Lobby 
            { 
                lobbyID = 100
            };

            coordinator.RegisterLobbyMapping(code, lobby);
            bool found = coordinator.TryGetLobbyIdFromCode(code, out int id);
            Assert.IsTrue(found);
            Assert.AreEqual(100, id);
        }

        [TestMethod]
        public void TestRemoveLobbyMappingShouldClearMapping()
        {
            string code = "MATCH1";
            
            var lobby = new Lobby 
            {
                lobbyID = 100 
            };

            coordinator.RegisterLobbyMapping(code, lobby);
            coordinator.RemoveLobbyMapping(code);
            bool found = coordinator.TryGetLobbyIdFromCode(code, out _);
            Assert.IsFalse(found);
        }

        [TestMethod]
        public void TestGetOrCreateLobbyLockShouldReturnSameObjectForSameId()
        {
            int lobbyId = 5;
            object lock1 = coordinator.GetOrCreateLobbyLock(lobbyId);
            object lock2 = coordinator.GetOrCreateLobbyLock(lobbyId);
            Assert.AreSame(lock1, lock2);
        }

        [TestMethod]
        public void TestRegisterChatCallbackShouldStoreCallback()
        {
            string code = "MATCH1";
            string player = "Player1";

            coordinator.RegisterLobbyMapping(code, new Lobby 
            { 
                lobbyID = 1 
            });

            bool result = coordinator.RegisterChatCallback(code, player, mockCallback.Object);
            Assert.IsTrue(result);
            var info = coordinator.GetPlayerInfoFromCallback(mockCallback.Object);
            Assert.IsNotNull(info);
            Assert.AreEqual(player, info.Username);
        }

        [TestMethod]
        public void TestGetGuestPlayersFromMemoryShouldReturnGuestsOnly()
        {
            string code = "MATCH1";
            
            coordinator.RegisterLobbyMapping(code, new Lobby 
            { 
                lobbyID = 1 
            });

            var mockGuest = new Mock<ITrucoCallback>();
            var mockUser = new Mock<ITrucoCallback>();
            coordinator.RegisterChatCallback(code, "Guest_123", mockGuest.Object);
            coordinator.RegisterChatCallback(code, "User_123", mockUser.Object);
            var guests = coordinator.GetGuestPlayersFromMemory(code);

            Assert.AreEqual(1, guests.Count);
            Assert.AreEqual("Guest_123", guests[0].Username);
        }

        [TestMethod]
        public void TestRemoveCallbackFromMatchShouldRemoveEntry()
        {
            string code = "MATCH1";
            
            coordinator.RegisterLobbyMapping(code, new Lobby 
            {
                lobbyID = 1 
            });

            coordinator.RegisterChatCallback(code, "Player1", mockCallback.Object);
            coordinator.RemoveCallbackFromMatch(code, mockCallback.Object);
            coordinator.TryGetCallbacksSnapshot(code, out var snapshot);
            Assert.AreEqual(0, snapshot.Length);
        }
    }
}
