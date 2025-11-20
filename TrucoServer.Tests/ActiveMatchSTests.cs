using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ActiveMatchSTests
    {
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var match = new ActiveMatch
            {
                Code = "TESTCODE",
                MatchDatabaseId = 1
            };

            var serializer = new XmlSerializer(typeof(ActiveMatch));

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, match);

                Assert.IsTrue(stream.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectCode()
        {
            var original = new ActiveMatch
            {
                Code = "EDYZ999"
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
                MatchDatabaseId = 100 
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