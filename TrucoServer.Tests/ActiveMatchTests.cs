using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ActiveMatchTests
    {
        [TestMethod]
        public void TestCodeSetReturnsCorrectString()
        {
            var match = new ActiveMatch();
            string expected = "MATCH_123";

            match.Code = expected;

            Assert.AreEqual(expected, match.Code);
        }

        [TestMethod]
        public void TestPlayersInitializeReturnsNotNull()
        {
            var match = new ActiveMatch();

            Assert.IsNotNull(match.Players);
        }

        [TestMethod]
        public void TestPlayersInitializeReturnsEmptyList()
        {
            var match = new ActiveMatch();

            Assert.AreEqual(0, match.Players.Count);
        }

        [TestMethod]
        public void TestTableCardsInitializeReturnsNotNull()
        {
            var match = new ActiveMatch();

            Assert.IsNotNull(match.TableCards);
        }

        [TestMethod]
        public void TestTableCardsInitializeReturnsEmptyList()
        {
            var match = new ActiveMatch();

            Assert.AreEqual(0, match.TableCards.Count);
        }

        [TestMethod]
        public void TestIsHandInProgressSetReturnsTrue()
        {
            var match = new ActiveMatch();

            match.IsHandInProgress = true;

            Assert.IsTrue(match.IsHandInProgress);
        }

        [TestMethod]
        public void TestCurrentTurnIndexSetReturnsCorrectInt()
        {
            var match = new ActiveMatch();
            int expected = 2;

            match.CurrentTurnIndex = expected;

            Assert.AreEqual(expected, match.CurrentTurnIndex);
        }

        [TestMethod]
        public void TestCurrentCallSetReturnsCorrectString()
        {
            var match = new ActiveMatch();
            string expected = "Truco";

            match.CurrentCall = expected;

            Assert.AreEqual(expected, match.CurrentCall);
        }

        [TestMethod]
        public void TestMatchDatabaseIdSetReturnsCorrectInt()
        {
            var match = new ActiveMatch();
            int expected = 55;

            match.MatchDatabaseId = expected;

            Assert.AreEqual(expected, match.MatchDatabaseId);
        }

        [TestMethod]
        public void TestCodeSetNullReturnsNull()
        {
            var match = new ActiveMatch();

            match.Code = null;

            Assert.IsNull(match.Code);
        }
    }
}