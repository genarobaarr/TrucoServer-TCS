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
        public void TestSetWinsShouldStoreValue()
        {
            var stats = new PlayerStats();
            stats.Wins = 10;
            Assert.AreEqual(10, stats.Wins);
        }

        [TestMethod]
        public void TestZeroLossesShouldBeValid()
        {
            var stats = new PlayerStats();
            stats.Losses = 0;
            Assert.AreEqual(0, stats.Losses);
        }

        [TestMethod]
        public void TestNegativeWinsShouldStoreNegative()
        {
            var stats = new PlayerStats();
            stats.Wins = -5;
            Assert.AreEqual(-5, stats.Wins);
        }

        [TestMethod]
        public void TestLongNameShouldPersist()
        {
            var stats = new PlayerStats();
            string longName = new string('a', 100);
            stats.PlayerName = longName;
            Assert.AreEqual(100, stats.PlayerName.Length);
        }

        [TestMethod]
        public void TestDefaultIntsShouldBeZero()
        {
            var stats = new PlayerStats();
            int total = stats.Wins + stats.Losses;
            Assert.AreEqual(0, total);
        }
    }
}
