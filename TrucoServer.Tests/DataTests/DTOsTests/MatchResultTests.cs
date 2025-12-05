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
        public void TestPlayer1SetStringReturnsString()
        {
            var result = new MatchResult();
            string p1 = "PlayerOne";
            result.Player1 = p1;
            Assert.AreEqual(p1, result.Player1);
        }

        [TestMethod]
        public void TestWinnerSetStringReturnsString()
        {
            var result = new MatchResult();
            string winner = "PlayerOne";
            result.Winner = winner;
            Assert.AreEqual(winner, result.Winner);
        }

        [TestMethod]
        public void TestDateSetStringReturnsString()
        {
            var result = new MatchResult();
            string date = "2023-01-01";
            result.Date = date;
            Assert.AreEqual(date, result.Date);
        }

        [TestMethod]
        public void TestPlayer2SetNullReturnsNull()
        {
            var result = new MatchResult();
            result.Player2 = null;
            Assert.IsNull(result.Player2);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var result = new MatchResult();
            Assert.IsNotNull(result);
        }
    }
}
