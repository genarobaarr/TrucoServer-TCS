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
    public class PlayerInfoSTests
    {
        private const string TEST_USERNAME = "TestUser";
        private const string TEST_SECOND_USERNAME = "SerializeMe";
        private const string TEST_TEAM_1 = "Team 1";
        private const int TEST_EMPTY_STREAM_LENGTH = 0;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var playerInfo = new PlayerInfo { Username = TEST_USERNAME };
            var serializer = new DataContractSerializer(typeof(PlayerInfo));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, playerInfo);

                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUsername()
        {
            var original = new PlayerInfo { Username = TEST_SECOND_USERNAME };
            var serializer = new DataContractSerializer(typeof(PlayerInfo));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PlayerInfo)serializer.ReadObject(stream);

                Assert.AreEqual(original.Username, result.Username);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectTeam()
        {
            var original = new PlayerInfo { Team = TEST_TEAM_1 };
            var serializer = new DataContractSerializer(typeof(PlayerInfo));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PlayerInfo)serializer.ReadObject(stream);

                Assert.AreEqual(original.Team, result.Team);
            }
        }
    }
}