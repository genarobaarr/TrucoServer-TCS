using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.Serialization;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class FriendDataSTests
    {
        private const string TEST_USERNAME = "test";
        private const string TEST_AVATAR_ID = "avatar_aaa_default";
        private const int TEST_POSITION_START = 0;

        private FriendData GetSampleFriendDataS()
        {
            return new FriendData
            {
                Username = TEST_USERNAME,
                AvatarId = TEST_AVATAR_ID
            };
        }

        private FriendData SerializeAndDeserialize(FriendData original)
        {
            var serializer = new DataContractSerializer(typeof(FriendData));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, original);
                ms.Position = TEST_POSITION_START;
                return (FriendData)serializer.ReadObject(ms);
            }
        }

        [TestMethod]
        public void TestFriendDataSerializationUsernameMatch()
        {
            var original = GetSampleFriendDataS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Username, copy.Username);
        }

        [TestMethod]
        public void TestFriendDataSerializationAvatarIdMatch()
        {
            var original = GetSampleFriendDataS();
            var copy = SerializeAndDeserialize(original);
            Assert.AreEqual(original.AvatarId, copy.AvatarId);
        }
    }
}
