using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class ListPositionForMatchTests
    {
        [TestMethod]
        public void TestDetermineTurnOrderReturnsEmptyListIfInputIsEmpty()
        {
            var emptyList = new List<PlayerInformation>();
            var result = ListPositionService.DetermineTurnOrder(emptyList, "Host");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestDetermineTurnOrderSetsHostFirstForDuel()
        {
            var p1 = new PlayerInformation(1, "Host", "Team 1");
            var p2 = new PlayerInformation(2, "Guest", "Team 2");
           
            var players = new List<PlayerInformation> 
            {
                p2,
                p1 
            };

            var result = ListPositionService.DetermineTurnOrder(players, "Host");
            Assert.AreEqual("Host", result[0].Username);
        }

        [TestMethod]
        public void TestDetermineTurnOrderSetsGuestSecondForDuel()
        {
            var p1 = new PlayerInformation(1, "Host", "Team 1");
            var p2 = new PlayerInformation(2, "Guest", "Team 2");
           
            var players = new List<PlayerInformation> 
            {
                p2, 
                p1 
            };

            var result = ListPositionService.DetermineTurnOrder(players, "Host");
            Assert.AreEqual("Guest", result[1].Username);
        }

        [TestMethod]
        public void TestDetermineTurnOrderSetsHostFirstForTeamMatch()
        {
            var host = new PlayerInformation(1, "Host", "Team 1");
            var mate = new PlayerInformation(2, "Teammate", "Team 1");
            var enemy1 = new PlayerInformation(3, "Alpha", "Team 2");
            var enemy2 = new PlayerInformation(4, "Zeta", "Team 2");

            var players = new List<PlayerInformation> 
            {
                mate,
                enemy2, 
                host, 
                enemy1 
            };

            var result = ListPositionService.DetermineTurnOrder(players, "Host");
            Assert.AreEqual("Host", result[0].Username);
        }

        [TestMethod]
        public void TestDetermineTurnOrderSetsRivalSecondForTeamMatch()
        {
            var host = new PlayerInformation(1, "Host", "Team 1");
            var mate = new PlayerInformation(2, "Teammate", "Team 1");
            var enemy1 = new PlayerInformation(3, "Alpha", "Team 2");
            var enemy2 = new PlayerInformation(4, "Zeta", "Team 2");

            var players = new List<PlayerInformation> 
            {
                mate,
                enemy2, 
                host,
                enemy1 
            };

            var result = ListPositionService.DetermineTurnOrder(players, "Host");
            Assert.AreEqual("Alpha", result[1].Username);
        }
    }
}
