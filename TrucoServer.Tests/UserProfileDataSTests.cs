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
    public class UserProfileDataSTests
    {
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var userProfile = new UserProfileData
            {
                Username = "TestUser",
                Email = "test@email.com",
                NameChangeCount = 1,
                EmblemLayers = new List<EmblemLayer>()
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, userProfile);
                byte[] data = stream.ToArray();

                Assert.IsTrue(data.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUsername()
        {
            var originalProfile = new UserProfileData
            {
                Username = "SerializerTest",
                XHandle = "@test"
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalProfile);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedProfile = (UserProfileData)serializer.ReadObject(stream);

                Assert.AreEqual(originalProfile.Username, deserializedProfile.Username);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectXHandle()
        {
            var originalProfile = new UserProfileData
            {
                Username = "SerializerTest",
                XHandle = "@test"
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalProfile);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedProfile = (UserProfileData)serializer.ReadObject(stream);

                Assert.AreEqual(originalProfile.XHandle, deserializedProfile.XHandle);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectEmblemLayersCount()
        {
            var originalProfile = new UserProfileData
            {
                EmblemLayers = new List<EmblemLayer>
                {
                    new EmblemLayer { ShapeId = 1, ColorHex = "#000000" }
                }
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalProfile);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedProfile = (UserProfileData)serializer.ReadObject(stream);

                Assert.AreEqual(originalProfile.EmblemLayers.Count, deserializedProfile.EmblemLayers.Count);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectEmblemLayerColor()
        {
            var originalProfile = new UserProfileData
            {
                EmblemLayers = new List<EmblemLayer>
                {
                    new EmblemLayer { ShapeId = 1, ColorHex = "#000000" }
                }
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));
            byte[] serializedData;

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, originalProfile);
                serializedData = stream.ToArray();
            }

            using (var stream = new MemoryStream(serializedData))
            {
                var deserializedProfile = (UserProfileData)serializer.ReadObject(stream);

                Assert.AreEqual(originalProfile.EmblemLayers[0].ColorHex, deserializedProfile.EmblemLayers[0].ColorHex);
            }
        }
    }
}