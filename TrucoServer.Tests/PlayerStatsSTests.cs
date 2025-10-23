using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerStatsSTests
    {
        private PlayerStats GetSamplePlayerStatsS()
        {
            return new PlayerStats
            {
                PlayerName = "test",
                Wins = 10,
                Losses = 3
            };
        }

        [TestMethod]
        public void PlayerStatsSerialization_PlayerName_Match()
        {
            var original = GetSamplePlayerStatsS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<PlayerStats>(json);
            Assert.AreEqual(original.PlayerName, copy.PlayerName);
        }

        [TestMethod]
        public void PlayerStatsSerialization_Wins_Match()
        {
            var original = GetSamplePlayerStatsS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<PlayerStats>(json);
            Assert.AreEqual(original.Wins, copy.Wins);
        }

        [TestMethod]
        public void PlayerStatsSerialization_Losses_Match()
        {
            var original = GetSamplePlayerStatsS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<PlayerStats>(json);
            Assert.AreEqual(original.Losses, copy.Losses);
        }
    }
}
