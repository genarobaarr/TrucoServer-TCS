using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class MatchScoreSTests
    {
        private const string TEST_MATCH_ID = "TEST_MATCH_001";
        private const int TEST_YEAR = 2023;
        private const int TEST_MONTH = 5;
        private const int TEST_DAY = 15;
        private const int TEST_FINAL_SCORE = 30;
        private const int TEST_EMPTY_STREAM_LENGTH = 0;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var score = new MatchScore
            {
                MatchID = TEST_MATCH_ID,
                FinalScore = TEST_FINAL_SCORE,
                IsWin = true,
                EndedAt = DateTime.Now
            };
            var serializer = new DataContractSerializer(typeof(MatchScore));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, score);
                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectMatchID()
        {
            var original = new MatchScore { MatchID = TEST_MATCH_ID };
            var serializer = new DataContractSerializer(typeof(MatchScore));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var result = (MatchScore)serializer.ReadObject(stream);
                Assert.AreEqual(original.MatchID, result.MatchID);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectFinalScore()
        {
            var original = new MatchScore { FinalScore = TEST_FINAL_SCORE };
            var serializer = new DataContractSerializer(typeof(MatchScore));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var result = (MatchScore)serializer.ReadObject(stream);
                Assert.AreEqual(original.FinalScore, result.FinalScore);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectIsWin()
        {
            var original = new MatchScore { IsWin = true };
            var serializer = new DataContractSerializer(typeof(MatchScore));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var result = (MatchScore)serializer.ReadObject(stream);
                Assert.IsTrue(result.IsWin);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectEndedAt()
        {
            var original = new MatchScore { EndedAt = new DateTime(TEST_YEAR, TEST_MONTH, TEST_DAY) };
            var serializer = new DataContractSerializer(typeof(MatchScore));
            byte[] data;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }
            using (var stream = new MemoryStream(data))
            {
                var result = (MatchScore)serializer.ReadObject(stream);
                Assert.AreEqual(original.EndedAt, result.EndedAt);
            }
        }
    }
}