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
    public class PublicLobyInfoSTests
    {
        private const string TEST_MATCH_USERNAME = "Test";
        private const string TEST_MATCH_CODE = "XYZ999";
        private const int TEST_MAX_PLAYERS = 4;
        private const int TEST_EMPTY_STREAM_LENGTH = 0;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var lobby = new PublicLobbyInfo { MatchName = TEST_MATCH_USERNAME };
            var serializer = new DataContractSerializer(typeof(PublicLobbyInfo));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, lobby);

                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectMatchCode()
        {
            var original = new PublicLobbyInfo { MatchCode = TEST_MATCH_CODE };
            var serializer = new DataContractSerializer(typeof(PublicLobbyInfo));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PublicLobbyInfo)serializer.ReadObject(stream);

                Assert.AreEqual(original.MatchCode, result.MatchCode);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectMaxPlayers()
        {
            var original = new PublicLobbyInfo { MaxPlayers = TEST_MAX_PLAYERS };
            var serializer = new DataContractSerializer(typeof(PublicLobbyInfo));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PublicLobbyInfo)serializer.ReadObject(stream);

                Assert.AreEqual(original.MaxPlayers, result.MaxPlayers);
            }
        }
    }
}
