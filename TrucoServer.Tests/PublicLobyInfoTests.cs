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
        [TestMethod]
        public void TestMatchNameSetReturnsCorrectString()
        {
            var lobby = new PublicLobbyInfo();
            string expected = "Fun Match";

            lobby.MatchName = expected;

            Assert.AreEqual(expected, lobby.MatchName);
        }

        [TestMethod]
        public void TestMatchCodeSetReturnsCorrectString()
        {
            var lobby = new PublicLobbyInfo();
            string expected = "CODE123";

            lobby.MatchCode = expected;

            Assert.AreEqual(expected, lobby.MatchCode);
        }

        [TestMethod]
        public void TestCurrentPlayersSetReturnsCorrectInt()
        {
            var lobby = new PublicLobbyInfo();
            int expected = 2;

            lobby.CurrentPlayers = expected;

            Assert.AreEqual(expected, lobby.CurrentPlayers);
        }

        [TestMethod]
        public void TestMaxPlayersSetReturnsCorrectInt()
        {
            var lobby = new PublicLobbyInfo();
            int expected = 4;

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