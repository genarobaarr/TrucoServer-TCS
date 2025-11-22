using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerStatsSTests
    {
        private const string TEST_USERNAME = "TestUser";
        private const int TEST_USER_WINS = 10;
        private const int TEST_USER_LOSSES = 3;

        private PlayerStats GetSamplePlayerStatsS()
        {
            return new PlayerStats
            {
                PlayerName = TEST_USERNAME,
                Wins = TEST_USER_WINS,
                Losses = TEST_USER_LOSSES
            };
        }

        [TestMethod]
        public void TestPlayerStatsSerializationPlayerNameMatchTrue()
        {
            var original = GetSamplePlayerStatsS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<PlayerStats>(json);
            Assert.AreEqual(original.PlayerName, copy.PlayerName);
        }

        [TestMethod]
        public void TestPlayerStatsSerializationWinsMatchTrue()
        {
            var original = GetSamplePlayerStatsS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<PlayerStats>(json);
            Assert.AreEqual(original.Wins, copy.Wins);
        }

        [TestMethod]
        public void TestPlayerStatsSerializationLossesMatchTrue()
        {
            var original = GetSamplePlayerStatsS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<PlayerStats>(json);
            Assert.AreEqual(original.Losses, copy.Losses);
        }
    }
}
