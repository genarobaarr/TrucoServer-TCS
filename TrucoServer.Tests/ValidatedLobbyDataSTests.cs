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
    public class ValidatedLobbyDataSTests
    {
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var validatedData = new ValidatedLobbyData
            {
                Lobby = new Lobby(),
                Members = new List<LobbyMember>(),
                Guests = new List<PlayerInfo>()
            };
            var serializer = new XmlSerializer(typeof(ValidatedLobbyData));

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, validatedData);
                byte[] data = stream.ToArray();

                Assert.IsTrue(data.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsNotNullLobby()
        {
            var originalData = new ValidatedLobbyData
            {
                Lobby = new Lobby(),
                Members = new List<LobbyMember>(),
                Guests = new List<PlayerInfo>()
            };
            var serializer = new XmlSerializer(typeof(ValidatedLobbyData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, originalData);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedData = (ValidatedLobbyData)serializer.Deserialize(stream);

                Assert.IsNotNull(deserializedData.Lobby);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsNotNullMembers()
        {
            var originalData = new ValidatedLobbyData
            {
                Lobby = new Lobby(),
                Members = new List<LobbyMember>(),
                Guests = new List<PlayerInfo>()
            };
            var serializer = new XmlSerializer(typeof(ValidatedLobbyData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, originalData);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedData = (ValidatedLobbyData)serializer.Deserialize(stream);

                Assert.IsNotNull(deserializedData.Members);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsNotNullGuests()
        {
            var originalData = new ValidatedLobbyData
            {
                Lobby = new Lobby(),
                Members = new List<LobbyMember>(),
                Guests = new List<PlayerInfo>()
            };
            var serializer = new XmlSerializer(typeof(ValidatedLobbyData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, originalData);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedData = (ValidatedLobbyData)serializer.Deserialize(stream);

                Assert.IsNotNull(deserializedData.Guests);
            }
        }
    }
}