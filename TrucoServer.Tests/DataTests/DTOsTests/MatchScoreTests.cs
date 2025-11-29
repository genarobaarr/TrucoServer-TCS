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
        public void TestIsWinTrueShouldStoreTrue()
        {
            var score = new MatchScore();
            score.IsWin = true;
            Assert.IsTrue(score.IsWin);
        }

        [TestMethod]
        public void TestZeroScoreShouldBeValid()
        {
            var score = new MatchScore();
            score.FinalScore = 0;
            Assert.AreEqual(0, score.FinalScore);
        }

        [TestMethod]
        public void TestEndedAtShouldStoreDateTime()
        {
            var score = new MatchScore();
            DateTime now = DateTime.Now;
            score.EndedAt = now;
            Assert.AreEqual(now, score.EndedAt);
        }

        [TestMethod]
        public void TestNullIDShouldBeNull()
        {
            var score = new MatchScore();
            score.MatchID = null;
            Assert.IsNull(score.MatchID);
        }

        [TestMethod]
        public void TestHighScoreShouldStoreValue()
        {
            var score = new MatchScore();
            score.FinalScore = 30;
            Assert.AreEqual(30, score.FinalScore);
        }
    }
}
