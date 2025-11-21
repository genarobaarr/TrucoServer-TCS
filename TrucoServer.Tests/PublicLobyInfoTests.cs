using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PublicLobyInfoTests
    {
        private const string TEST_MATCH_USERNAME = "Test";
        private const string TEST_MATCH_CODE = "XYZ999";
        private const string TEST_MATCH_NAME = "Paleolitic";
        private const int TEST_MAX_PLAYERS = 4;
        private const int TEST_CURRENT_PLAYERS = 2;

        [TestMethod]
        public void TestMatchNameSetReturnsCorrectString()
        {
            var lobby = new PublicLobbyInfo();
            string expected = TEST_MATCH_NAME;

            lobby.MatchName = expected;

            Assert.AreEqual(expected, lobby.MatchName);
        }

        [TestMethod]
        public void TestMatchCodeSetReturnsCorrectString()
        {
            var lobby = new PublicLobbyInfo();
            string expected = TEST_MATCH_CODE;

            lobby.MatchCode = expected;

            Assert.AreEqual(expected, lobby.MatchCode);
        }

        [TestMethod]
        public void TestCurrentPlayersSetReturnsCorrectInt()
        {
            var lobby = new PublicLobbyInfo();
            int expected = TEST_CURRENT_PLAYERS;

            lobby.CurrentPlayers = expected;

            Assert.AreEqual(expected, lobby.CurrentPlayers);
        }

        [TestMethod]
        public void TestMaxPlayersSetReturnsCorrectInt()
        {
            var lobby = new PublicLobbyInfo();
            int expected = TEST_MAX_PLAYERS;

            lobby.MaxPlayers = expected;

            Assert.AreEqual(expected, lobby.MaxPlayers);
        }

        [TestMethod]
        public void TestMatchNameSetNullReturnsNull()
        {
            var lobby = new PublicLobbyInfo();

            lobby.MatchName = null;

            Assert.IsNull(lobby.MatchName);
        }
    }
}