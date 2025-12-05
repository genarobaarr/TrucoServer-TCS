using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class GameInitializationOptionsTests
    {
        [TestMethod]
        public void TestLobbyIdSetPositiveValueReturnsValue()
        {
            var options = new GameInitializationOptions();
            int lobbyId = 123;
            options.LobbyId = lobbyId;
            Assert.AreEqual(lobbyId, options.LobbyId);
        }

        [TestMethod]
        public void TestMatchCodeSetStringReturnsString()
        {
            var options = new GameInitializationOptions();
            string code = "ABC123";
            options.MatchCode = code;
            Assert.AreEqual(code, options.MatchCode);
        }

        [TestMethod]
        public void TestGamePlayersSetListReturnsList()
        {
            var options = new GameInitializationOptions();
            var players = new List<PlayerInformation>();
            options.GamePlayers = players;
            Assert.AreSame(players, options.GamePlayers);
        }

        [TestMethod]
        public void TestGameCallbacksSetDictionaryReturnsDictionary()
        {
            var options = new GameInitializationOptions();
            var callbacks = new Dictionary<int, ITrucoCallback>();
            options.GameCallbacks = callbacks;
            Assert.AreSame(callbacks, options.GameCallbacks);
        }

        [TestMethod]
        public void TestGamePlayersSetNullReturnsNull()
        {
            var options = new GameInitializationOptions();
            options.GamePlayers = null;
            Assert.IsNull(options.GamePlayers);
        }
    }
}