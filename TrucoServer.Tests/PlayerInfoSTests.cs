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
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var playerInfo = new PlayerInfo { Username = "TestUser" };
            var serializer = new DataContractSerializer(typeof(PlayerInfo));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, playerInfo);

                Assert.IsTrue(stream.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUsername()
        {
            var original = new PlayerInfo { Username = "SerializeMe" };
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
            var original = new PlayerInfo { Team = "Team 1" };
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