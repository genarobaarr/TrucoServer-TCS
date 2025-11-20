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
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var lobby = new PublicLobbyInfo { MatchName = "Test" };
            var serializer = new DataContractSerializer(typeof(PublicLobbyInfo));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, lobby);

                Assert.IsTrue(stream.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectMatchCode()
        {
            var original = new PublicLobbyInfo { MatchCode = "XYZ999" };
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
            var original = new PublicLobbyInfo { MaxPlayers = 6 };
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
