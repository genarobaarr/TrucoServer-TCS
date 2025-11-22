using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Serialization;
using TrucoServer.Data.Entities;

namespace TrucoServer.Tests
{
    [TestClass]
    public class FriendShipMappingSTests
    {
        private const int TEST_USER_ID = 1;
        private const int TEST_SECOND_USER_ID = 99;
        private const int TEST_FRIEND_ID = 2;
        private const int TEST_SECOND_FRIEND_ID = 100;
        private const int TEST_EMPTY_STREAM_LENGTH = 0;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var mapping = new FriendShipMapping 
            { 
                UserID = TEST_USER_ID, 
                FriendID = TEST_FRIEND_ID
            };
            var serializer = new XmlSerializer(typeof(FriendShipMapping));

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, mapping);

                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUserID()
        {
            var original = new FriendShipMapping
            { 
                UserID = TEST_SECOND_USER_ID, 
                FriendID = TEST_SECOND_FRIEND_ID 
            };
            var serializer = new XmlSerializer(typeof(FriendShipMapping));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (FriendShipMapping)serializer.Deserialize(stream);

                Assert.AreEqual(original.UserID, result.UserID);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectFriendID()
        {
            var original = new FriendShipMapping 
            { 
                UserID = TEST_SECOND_USER_ID, 
                FriendID = TEST_SECOND_FRIEND_ID 
            };
            var serializer = new XmlSerializer(typeof(FriendShipMapping));
            byte[] data;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, original);
                data = stream.ToArray();
            }

            using (var stream = new MemoryStream(data))
            {
                var result = (FriendShipMapping)serializer.Deserialize(stream);

                Assert.AreEqual(original.FriendID, result.FriendID);
            }
        }
    }
}