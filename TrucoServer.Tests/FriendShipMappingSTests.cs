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
    public class FriendShipMappingSTests
    {
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var mapping = new FriendShipMapping 
            { 
                userID = 1, 
                friendID = 2 
            };
            var serializer = new XmlSerializer(typeof(FriendShipMapping));

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, mapping);

                Assert.IsTrue(stream.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUserID()
        {
            var original = new FriendShipMapping
            { 
                userID = 99, 
                friendID = 100 
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

                Assert.AreEqual(original.userID, result.userID);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectFriendID()
        {
            var original = new FriendShipMapping 
            { 
                userID = 99, 
                friendID = 100 
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

                Assert.AreEqual(original.friendID, result.friendID);
            }
        }
    }
}