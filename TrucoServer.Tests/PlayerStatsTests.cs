using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerStatsTests
    {
        private const string TEST_USERNAME = "TestUser";
        private const int TEST_USER_WINS = 10;
        private const int TEST_USER_LOSSES = 3;
        private PlayerStats GetSamplePlayerStats()
        {
            return new PlayerStats
            {
                PlayerName = TEST_USERNAME,
                Wins = TEST_USER_WINS,
                Losses = TEST_USER_LOSSES
            };
        }

        [TestMethod]
        public void TestPlayerStatsPlayerNameBeTest()
        {
            var stats = GetSamplePlayerStats();
            Assert.AreEqual(TEST_USERNAME, stats.PlayerName);
        }

        [TestMethod]
        public void TestPlayerStatsWinsBe10()
        {
            var stats = GetSamplePlayerStats();
            Assert.AreEqual(TEST_USER_WINS, stats.Wins);
        }

        [TestMethod]
        public void TestPlayerStatsLossesBe3()
        {
            var stats = GetSamplePlayerStats();
            Assert.AreEqual(TEST_USER_LOSSES, stats.Losses);
        }
    }
}
