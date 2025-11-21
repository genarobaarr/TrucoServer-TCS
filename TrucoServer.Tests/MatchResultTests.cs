using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class MatchResultTests
    {
        private const string TEST_PLAYER1 = "test";
        private const string TEST_PLAYER2 = "test2";
        private const string TEST_WINNER = "test2";
        private const string TEST_DATE = "2025-10-22";
        private const int TEST_POSITION_ZERO = 0;

        private MatchResult GetSampleMatchResult()
        {
            return new MatchResult
            {
                Player1 = TEST_PLAYER1,
                Player2 = TEST_PLAYER2,
                Winner = TEST_WINNER,
                Date = TEST_DATE
            };
        }

        [TestMethod]
        public void TestMatchResultPlayer1BeTestTrue()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual(TEST_PLAYER1, match.Player1);
        }

        [TestMethod]
        public void TestMatchResultPlayer2BeTest2True()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual(TEST_PLAYER2, match.Player2);
        }

        [TestMethod]
        public void TestMatchResultWinnerBeTest2True()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual(TEST_WINNER, match.Winner);
        }

        [TestMethod]
        public void TestMatchResultDateBe20251022True()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual(TEST_DATE, match.Date);
        }
    }
}
