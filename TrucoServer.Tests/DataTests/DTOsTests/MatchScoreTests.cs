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
    public class MatchScoreTests
    {
        [TestMethod]
        public void TestMatchIDSetValidStringReturnsString()
        {
            var score = new MatchScore();
            string id = "100";
            score.MatchID = id;
            Assert.AreEqual(id, score.MatchID);
        }

        [TestMethod]
        public void TestEndedAtSetCurrentTimeReturnsTime()
        {
            var score = new MatchScore();
            DateTime now = DateTime.Now;
            score.EndedAt = now;
            Assert.AreEqual(now, score.EndedAt);
        }

        [TestMethod]
        public void TestIsWinSetTrueReturnsTrue()
        {
            var score = new MatchScore();
            score.IsWin = true;
            Assert.IsTrue(score.IsWin);
        }

        [TestMethod]
        public void TestFinalScoreSetIntegerReturnsInteger()
        {
            var score = new MatchScore();
            int points = 30;
            score.FinalScore = points;
            Assert.AreEqual(points, score.FinalScore);
        }

        [TestMethod]
        public void TestMatchIDSetNullReturnsNull()
        {
            var score = new MatchScore();
            score.MatchID = null;
            Assert.IsNull(score.MatchID);
        }
    }
}
