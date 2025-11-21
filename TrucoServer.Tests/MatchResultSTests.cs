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
    public class MatchResultSTests
    {
        private const string TEST_PLAYER1 = "test";
        private const string TEST_PLAYER2 = "test2";
        private const string TEST_WINNER = "test2";
        private const string TEST_DATE = "2025-10-22";
        private const int TEST_POSITION_ZERO = 0;

        private MatchResult GetSampleMatchResultS()
        {
            return new MatchResult
            {
                Player1 = TEST_PLAYER1,
                Player2 = TEST_PLAYER2,
                Winner = TEST_WINNER,
                Date = TEST_DATE
            };
        }

        private MatchResult SerializeAndDeserialize(MatchResult original)
        {
            var serializer = new DataContractSerializer(typeof(MatchResult));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, original);
                ms.Position = TEST_POSITION_ZERO;
                return (MatchResult)serializer.ReadObject(ms);
            }
        }

        [TestMethod]
        public void TestMatchResultSerializationPlayer1Match()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Player1, copy.Player1);
        }

        [TestMethod]
        public void TestMatchResultSerializationPlayer2Match()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Player2, copy.Player2);
        }

        [TestMethod]
        public void TestMatchResultSerializationWinnerMatch()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Winner, copy.Winner);
        }

        [TestMethod]
        public void TestMatchResultSerializationDateMatch()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Date, copy.Date);
        }
    }
}
