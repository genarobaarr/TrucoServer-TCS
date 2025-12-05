using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class PlayerStatsTests
    {
        [TestMethod]
        public void TestPlayerNameSetStringReturnsString()
        {
            var stats = new PlayerStats();
            string name = "WinnerPlayer";
            stats.PlayerName = name;
            Assert.AreEqual(name, stats.PlayerName);
        }

        [TestMethod]
        public void TestWinsSetPositiveValueReturnsValue()
        {
            var stats = new PlayerStats();
            int wins = 10;
            stats.Wins = wins;
            Assert.AreEqual(wins, stats.Wins);
        }

        [TestMethod]
        public void TestLossesSetPositiveValueReturnsValue()
        {
            var stats = new PlayerStats();
            int losses = 2;
            stats.Losses = losses;
            Assert.AreEqual(losses, stats.Losses);
        }
    }
}
