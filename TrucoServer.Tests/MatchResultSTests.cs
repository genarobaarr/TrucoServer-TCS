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
        private MatchResult GetSampleMatchResultS()
        {
            return new MatchResult
            {
                Player1 = "test",
                Player2 = "test2",
                Winner = "test2",
                Date = "2025-10-22"
            };
        }

        private MatchResult SerializeAndDeserialize(MatchResult original)
        {
            var serializer = new DataContractSerializer(typeof(MatchResult));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, original);
                ms.Position = 0;
                return (MatchResult)serializer.ReadObject(ms);
            }
        }

        [TestMethod]
        public void MatchResultSerializationPlayer1Match()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Player1, copy.Player1);
        }

        [TestMethod]
        public void MatchResultSerializationPlayer2Match()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Player2, copy.Player2);
        }

        [TestMethod]
        public void MatchResultSerializationWinnerMatch()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Winner, copy.Winner);
        }

        [TestMethod]
        public void MatchResultSerializationDateMatch()
        {
            var original = GetSampleMatchResultS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Date, copy.Date);
        }
    }
}
