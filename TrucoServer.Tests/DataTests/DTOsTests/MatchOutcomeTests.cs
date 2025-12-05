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
    public class MatchOutcomeTests
    {
        [TestMethod]
        public void TestWinnerTeamSetValidStringReturnsString()
        {
            var outcome = new MatchOutcome();
            string team = "Team 1";
            outcome.WinnerTeam = team;
            Assert.AreEqual(team, outcome.WinnerTeam);
        }

        [TestMethod]
        public void TestWinnerScoreSetPositiveValueReturnsValue()
        {
            var outcome = new MatchOutcome();
            int score = 30;
            outcome.WinnerScore = score;
            Assert.AreEqual(score, outcome.WinnerScore);
        }

        [TestMethod]
        public void TestLoserScoreSetZeroReturnsZero()
        {
            var outcome = new MatchOutcome();
            int score = 0;
            outcome.LoserScore = score;
            Assert.AreEqual(0, outcome.LoserScore);
        }

        [TestMethod]
        public void TestWinnerTeamSetNullReturnsNull()
        {
            var outcome = new MatchOutcome();
            outcome.WinnerTeam = null;
            Assert.IsNull(outcome.WinnerTeam);
        }

        [TestMethod]
        public void TestLoserScoreSetNegativeValueReturnsValue()
        {
            var outcome = new MatchOutcome();
            int negative = -10;
            outcome.LoserScore = negative;
            Assert.AreEqual(negative, outcome.LoserScore);
        }
    }
}
