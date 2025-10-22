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
        [TestMethod]
        public void MatchResultTestsTrue()
        {
            var match = new MatchResult
            {
                Player1 = "test",
                Player2 = "test2",
                Winner = "test",
                Date = "2025-10-22"
            };

            Assert.AreEqual("test", match.Player1);
            Assert.AreEqual("test2", match.Player2);
            Assert.AreEqual("test", match.Winner);
            Assert.AreEqual("2025-10-22", match.Date);
        }
    }
}
