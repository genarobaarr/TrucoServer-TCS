using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Serialization;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ActiveMatchSTests
    {
        private const string TEST_CODE = "EDYZ999";
        private const int TEST_MATCH_DATABASE_ID = 42;
        private const int TEST_MATCH_OTHER_DATABASE_ID = 100;
        private const int TEST_EMPTY_LIST = 0;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var match = new ActiveMatch
            {
                Code = TEST_CODE,
                MatchDatabaseId = TEST_MATCH_DATABASE_ID
            };

            var serializer = new XmlSerializer(typeof(ActiveMatch));

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, match);

                Assert.IsTrue(stream.Length > TEST_EMPTY_LIST);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectCode()
        {
            var original = new ActiveMatch
            {
                Code = TEST_CODE
            };

            var serializer = new XmlSerializer(typeof(ActiveMatch));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (ActiveMatch)serializer.Deserialize(stream);

                Assert.AreEqual(original.Code, result.Code);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectMatchDatabaseId()
        {
            var original = new ActiveMatch 
            { 
                MatchDatabaseId = TEST_MATCH_OTHER_DATABASE_ID 
            };

            var serializer = new XmlSerializer(typeof(ActiveMatch));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (ActiveMatch)serializer.Deserialize(stream);

                Assert.AreEqual(original.MatchDatabaseId, result.MatchDatabaseId);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsNotNullPlayersList()
        {
            var original = new ActiveMatch();
            var serializer = new XmlSerializer(typeof(ActiveMatch));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (ActiveMatch)serializer.Deserialize(stream);

                Assert.IsNotNull(result.Players);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectIsHandInProgress()
        {
            var original = new ActiveMatch 
            { 
                IsHandInProgress = true
            };

            var serializer = new XmlSerializer(typeof(ActiveMatch));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (ActiveMatch)serializer.Deserialize(stream);

                Assert.IsTrue(result.IsHandInProgress);
            }
        }
    }
}