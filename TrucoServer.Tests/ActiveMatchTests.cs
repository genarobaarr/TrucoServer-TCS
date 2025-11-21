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
        private const string TEST_CORRECT_STRING = "Truco";
        private const string TEST_EXPECTED_STRING = "MATCH_123";
        private const int TEST_EXPECTED_INT = 55;
        private const int TEST_SECOND_EXPECTED_INT = 2;
        private const int TEST_PLAYERS_EMPTY_LIST = 0;
        private const int TEST_CARDS_EMPTY_LIST = 0;

        [TestMethod]
        public void TestCodeSetReturnsCorrectString()
        {
            var match = new ActiveMatch();
            string expected = TEST_EXPECTED_STRING;

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

            Assert.AreEqual(TEST_PLAYERS_EMPTY_LIST, match.Players.Count);
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

            Assert.AreEqual(TEST_CARDS_EMPTY_LIST, match.TableCards.Count);
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
            int expected = TEST_SECOND_EXPECTED_INT;

            match.CurrentTurnIndex = expected;

            Assert.AreEqual(expected, match.CurrentTurnIndex);
        }

        [TestMethod]
        public void TestCurrentCallSetReturnsCorrectString()
        {
            var match = new ActiveMatch();
            string expected = TEST_CORRECT_STRING;

            match.CurrentCall = expected;

            Assert.AreEqual(expected, match.CurrentCall);
        }

        [TestMethod]
        public void TestMatchDatabaseIdSetReturnsCorrectInt()
        {
            var match = new ActiveMatch();
            int expected = TEST_EXPECTED_INT;

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