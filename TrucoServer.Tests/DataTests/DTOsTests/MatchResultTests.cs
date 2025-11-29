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
    public class MatchResultTests
    {
        [TestMethod]
        public void TestSetWinnerShouldStoreWinnerName()
        {
            var result = new MatchResult();
            string winner = "Jordan";
            result.Winner = winner;
            Assert.AreEqual(winner, result.Winner);
        }

        [TestMethod]
        public void TestSamePlayersShouldAllowAssignment()
        {
            var result = new MatchResult();
            string p1 = "Jordan";
            result.Player1 = p1;
            result.Player2 = p1;
            Assert.AreEqual(result.Player1, result.Player2);
        }

        [TestMethod]
        public void TestDateStringShouldStoreFormat()
        {
            var result = new MatchResult();
            string dateStr = "2023-10-25 10:00:00";
            result.Date = dateStr;
            Assert.AreEqual(dateStr, result.Date);
        }

        [TestMethod]
        public void TestNullPlayerShouldBeNull()
        {
            var result = new MatchResult();
            result.Player2 = null;
            Assert.IsNull(result.Player2);
        }

        [TestMethod]
        public void TestNoWinnerShouldBeNullByDefault()
        {
            var result = new MatchResult();
            Assert.IsNull(result.Winner);
        }
    }
}
