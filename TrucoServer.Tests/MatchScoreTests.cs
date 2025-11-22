using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class MatchScoreTests
    {
        private const string TEST_MATCH_ID = "TEST_MATCH_001";
        private const int TEST_YEAR = 2023;
        private const int TEST_MONTH = 5;
        private const int TEST_DAY = 15;
        private const int TEST_FINAL_SCORE = 30;

        [TestMethod]
        public void TestMatchIDSetReturnsCorrectString()
        {
            var score = new MatchScore();
            string expected = TEST_MATCH_ID;
            score.MatchID = expected;
            Assert.AreEqual(expected, score.MatchID);
        }

        [TestMethod]
        public void TestEndedAtSetReturnsCorrectDateTime()
        {
            var score = new MatchScore();
            var expected = new DateTime(TEST_YEAR, TEST_MONTH, TEST_DAY);
            score.EndedAt = expected;
            Assert.AreEqual(expected, score.EndedAt);
        }

        [TestMethod]
        public void TestIsWinSetReturnsTrue()
        {
            var score = new MatchScore();
            score.IsWin = true;
            Assert.IsTrue(score.IsWin);
        }

        [TestMethod]
        public void TestFinalScoreSetReturnsCorrectInt()
        {
            var score = new MatchScore();
            int expected = TEST_FINAL_SCORE;
            score.FinalScore = expected;
            Assert.AreEqual(expected, score.FinalScore);
        }

        [TestMethod]
        public void TestMatchIDSetNullReturnsNull()
        {
            var score = new MatchScore();
            score.MatchID = null;
            Assert.IsNull(score.MatchID);
        }

        [TestMethod]
        public void TestFinalScoreSetBoundaryValueReturnsCorrectInt()
        {
            var score = new MatchScore();
            int expected = int.MaxValue;
            score.FinalScore = expected;
            Assert.AreEqual(expected, score.FinalScore);
        }
    }
}
