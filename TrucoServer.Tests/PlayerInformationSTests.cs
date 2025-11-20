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
    public class PlayerInformationSTests
    {
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var player = new PlayerInformation(5, "Test", "A");
            var serializer = new DataContractSerializer(typeof(PlayerInformation));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, player);

                Assert.IsTrue(stream.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectPlayerID()
        {
            var original = new PlayerInformation(99, "Test", "A");
            var serializer = new DataContractSerializer(typeof(PlayerInformation));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PlayerInformation)serializer.ReadObject(stream);

                Assert.AreEqual(original.PlayerID, result.PlayerID);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUsername()
        {
            var original = new PlayerInformation(99, "UniqueName", "A");
            var serializer = new DataContractSerializer(typeof(PlayerInformation));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PlayerInformation)serializer.ReadObject(stream);

                Assert.AreEqual(original.Username, result.Username);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsNotNullHand()
        {
            var original = new PlayerInformation(1, "Test", "A");
            var serializer = new DataContractSerializer(typeof(PlayerInformation));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (PlayerInformation)serializer.ReadObject(stream);

                Assert.IsNotNull(result.Hand);
            }
        }
    }
}