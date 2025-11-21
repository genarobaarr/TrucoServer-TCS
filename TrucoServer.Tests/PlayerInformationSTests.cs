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
        private const string TEST_NAME = "Test";
        private const string TEST_SUIT = "A";
        private const int TEST_EMPTY_STREAM = 0;
        private const int TEST_ID = 5;
        private const int TEST_SECOND_ID = 99;
        private const string TEST_UNIQUE_NAME = "UniqueName";

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var player = new PlayerInformation(TEST_ID, TEST_NAME, TEST_SUIT);
            var serializer = new DataContractSerializer(typeof(PlayerInformation));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, player);

                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectPlayerID()
        {
            var original = new PlayerInformation(TEST_SECOND_ID, TEST_NAME, TEST_SUIT);
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
            var original = new PlayerInformation(TEST_SECOND_ID, TEST_UNIQUE_NAME, TEST_SUIT);
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
            var original = new PlayerInformation(TEST_ID, TEST_NAME, TEST_SUIT);
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