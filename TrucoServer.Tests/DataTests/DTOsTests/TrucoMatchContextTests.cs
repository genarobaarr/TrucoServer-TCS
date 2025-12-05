using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class TrucoMatchContextTests
    {
        [TestMethod]
        public void TestMatchCodeSetValidCodeReturnsCode()
        {
            var context = new TrucoMatchContext();
            string code = "MACH999";
            context.MatchCode = code;
            Assert.AreEqual(code, context.MatchCode);
        }

        [TestMethod]
        public void TestLobbyIdSetPositiveValueReturnsValue()
        {
            var context = new TrucoMatchContext();
            int id = 50;
            context.LobbyId = id;
            Assert.AreEqual(id, context.LobbyId);
        }

        [TestMethod]
        public void TestPlayersSetListReturnsList()
        {
            var context = new TrucoMatchContext();
            var players = new List<PlayerInformation>();
            context.Players = players;
            Assert.AreSame(players, context.Players);
        }
    }
}
