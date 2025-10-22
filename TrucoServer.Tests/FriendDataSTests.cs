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
        [TestMethod]
        public void FriendDataSerializationTests()
        {
            var original = new FriendData
            {
                Username = "test",
                AvatarId = "avatar_aaa_default"
            };

            var serializer = new DataContractSerializer(typeof(FriendData));
            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, original);
                memoryStream.Position = 0;

                var deserialized = (FriendData)serializer.ReadObject(memoryStream);

                Assert.AreEqual(original.Username, deserialized.Username);
                Assert.AreEqual(original.AvatarId, deserialized.AvatarId);
            }
        }
    }
}
