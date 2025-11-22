using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class UserProfileDataSTests
    {
        private const string TEST_USERNAME = "DeMarius";
        private const string TEST_EMAIL_ADDRESS = "test@gmail.com";
        private const string TEST_EMBLEM = "#000000";
        private const string TEST_X_HANDLE = "@demarius";
        private const int TEST_NAME_CHANGE_COUNT = 2;
        private const int TEST_EMPTY_STREAM_LENGTH = 0;
        private const int TEST_SHAPE_ID = 1;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var userProfile = new UserProfileData
            {
                Username = TEST_USERNAME,
                Email = TEST_EMAIL_ADDRESS,
                NameChangeCount = TEST_NAME_CHANGE_COUNT,
                EmblemLayers = new List<EmblemLayer>()
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, userProfile);
                byte[] data = stream.ToArray();

                Assert.IsTrue(data.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectUsername()
        {
            var originalProfile = new UserProfileData
            {
                Username = TEST_USERNAME,
                XHandle = TEST_X_HANDLE
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
                Username = TEST_USERNAME,
                XHandle = TEST_X_HANDLE
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
                    new EmblemLayer { ShapeId = TEST_SHAPE_ID, ColorHex = TEST_EMBLEM }
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
                    new EmblemLayer { ShapeId = TEST_SHAPE_ID, ColorHex = TEST_EMBLEM }
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