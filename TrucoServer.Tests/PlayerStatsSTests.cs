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
        [TestMethod]
        public void PlayerStatsSerializationTests()
        {
            var original = new PlayerStats
            {
                PlayerName = "test",
                Wins = 10,
                Losses = 3
            };
            string json = JsonConvert.SerializeObject(original);
            var copia = JsonConvert.DeserializeObject<PlayerStats>(json);

            Assert.AreEqual(original.PlayerName, copia.PlayerName);
            Assert.AreEqual(original.Wins, copia.Wins);
            Assert.AreEqual(original.Losses, copia.Losses);
        }
    }
}
