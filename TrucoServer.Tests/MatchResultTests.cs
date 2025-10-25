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
        private MatchResult GetSampleMatchResult()
        {
            return new MatchResult
            {
                Player1 = "test",
                Player2 = "test2",
                Winner = "test2",
                Date = "2025-10-22"
            };
        }

        [TestMethod]
        public void TestMatchResultPlayer1BeTestTrue()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual("test", match.Player1);
        }

        [TestMethod]
        public void TestMatchResultPlayer2BeTest2True()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual("test2", match.Player2);
        }

        [TestMethod]
        public void TestMatchResultWinnerBeTest2True()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual("test2", match.Winner);
        }

        [TestMethod]
        public void TestMatchResultDateBe20251022True()
        {
            var match = GetSampleMatchResult();
            Assert.AreEqual("2025-10-22", match.Date);
        }
    }
}
