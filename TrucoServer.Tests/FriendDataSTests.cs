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
    public class FriendDataSTests
    {
        private FriendData GetSampleFriendDataS()
        {
            return new FriendData
            {
                Username = "test",
                AvatarId = "avatar_aaa_default"
            };
        }

        private FriendData SerializeAndDeserialize(FriendData original)
        {
            var serializer = new DataContractSerializer(typeof(FriendData));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, original);
                ms.Position = 0;
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
