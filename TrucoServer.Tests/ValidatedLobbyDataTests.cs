using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ValidatedLobbyDataTests
    {
        private const int TEST_EMPTY_LIST_LENGTH = 0;
        private const int TEST_CORRECT_LIST_COUNT = 2;

        [TestMethod]
        public void TestLobbyPropertySetReturnsCorrectObject()
        {
            var validatedLobby = new ValidatedLobbyData();
            var lobby = new Lobby();

            validatedLobby.Lobby = lobby;

            Assert.AreEqual(lobby, validatedLobby.Lobby);
        }

        [TestMethod]
        public void TestMembersListInitializeReturnsEmptyList()
        {
            var validatedLobby = new ValidatedLobbyData();
            var members = new List<LobbyMember>();

            validatedLobby.Members = members;

            Assert.AreEqual(TEST_EMPTY_LIST_LENGTH, validatedLobby.Members.Count);
        }

        [TestMethod]
        public void TestGuestsListInitializeReturnsNull()
        {
            var validatedLobby = new ValidatedLobbyData();

            Assert.IsNull(validatedLobby.Guests);
        }

        [TestMethod]
        public void TestGuestsListSetReturnsCorrectCount()
        {
            var validatedLobby = new ValidatedLobbyData();
            var guests = new List<PlayerInfo> { new PlayerInfo(), new PlayerInfo() };

            validatedLobby.Guests = guests;

            Assert.AreEqual(TEST_CORRECT_LIST_COUNT, validatedLobby.Guests.Count);
        }

        [TestMethod]
        public void TestLobbySetNullReturnsNull()
        {
            var validatedLobby = new ValidatedLobbyData();

            validatedLobby.Lobby = null;

            Assert.IsNull(validatedLobby.Lobby);
        }

        [TestMethod]
        public void TestMembersSetNullReturnsNull()
        {
            var validatedLobby = new ValidatedLobbyData();

            validatedLobby.Members = null;

            Assert.IsNull(validatedLobby.Members);
        }

        [TestMethod]
        public void TestGuestsSetNullReturnsNull()
        {
            var validatedLobby = new ValidatedLobbyData();

            validatedLobby.Guests = null;

            Assert.IsNull(validatedLobby.Guests);
        }
    }
}