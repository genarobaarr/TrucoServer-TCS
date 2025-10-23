using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerStatsTests
    {
        private PlayerStats GetSamplePlayerStats()
        {
            return new PlayerStats
            {
                PlayerName = "test",
                Wins = 10,
                Losses = 3
            };
        }

        [TestMethod]
        public void PlayerStatsPlayerNameBeTest()
        {
            var stats = GetSamplePlayerStats();
            Assert.AreEqual("test", stats.PlayerName);
        }

        [TestMethod]
        public void PlayerStatsWinsBe10()
        {
            var stats = GetSamplePlayerStats();
            Assert.AreEqual(10, stats.Wins);
        }

        [TestMethod]
        public void PlayerStatsLossesBe3()
        {
            var stats = GetSamplePlayerStats();
            Assert.AreEqual(3, stats.Losses);
        }
    }
}
