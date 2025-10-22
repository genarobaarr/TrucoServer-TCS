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
        [TestMethod]
        public void CreatePlayerStatsTest()
        {
            var stats = new PlayerStats
            {
                PlayerName = "test",
                Wins = 5,
                Losses = 2
            };
            Assert.AreEqual("test", stats.PlayerName);
            Assert.AreEqual(5, stats.Wins);
            Assert.AreEqual(2, stats.Losses);
        }
    }
}
