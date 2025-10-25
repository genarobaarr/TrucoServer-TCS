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
        private UserProfileData GetSampleUserProfile()
        {
            return new UserProfileData
            {
                Username = "TestUser",
                Email = "test@example.com",
                AvatarId = "avatar_aaa_default"
            };
        }

        private UserProfileData SerializeAndDeserialize(UserProfileData original)
        {
            var serializer = new DataContractSerializer(typeof(UserProfileData));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, original);
                ms.Position = 0;
                return (UserProfileData)serializer.ReadObject(ms);
            }
        }

        [TestMethod]
        public void TestUserProfileDataSerializationUsernameMatch()
        {
            var original = GetSampleUserProfile();
            var copia = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Username, copia.Username);
        }

        [TestMethod]
        public void TestUserProfileDataSerializationEmailMatch()
        {
            var original = GetSampleUserProfile();
            var copia = SerializeAndDeserialize(original);
            Assert.AreEqual(original.Email, copia.Email);
        }

        [TestMethod]
        public void TestUserProfileDataSerializationAvatarIdMatch()
        {
            var original = GetSampleUserProfile();
            var copia = SerializeAndDeserialize(original);
            Assert.AreEqual(original.AvatarId, copia.AvatarId);
        }
    }
}
