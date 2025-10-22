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
    public class UserProfileDataSTest
    {
        [TestMethod]
        public void UserProfileDataSerializationTest()
        {
            var original = new UserProfileData
            {
                Username = "TestUser",
                Email = "test@example.com",
                AvatarId = "avatar_aaa_default"
            };
            var serializer = new DataContractSerializer(typeof(UserProfileData));
            UserProfileData copia;

            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, original);
                ms.Position = 0; 
                copia = (UserProfileData)serializer.ReadObject(ms);
            }

            Assert.AreEqual(original.Username, copia.Username);
            Assert.AreEqual(original.Email, copia.Email);
            Assert.AreEqual(original.AvatarId, copia.AvatarId);
        }
    }
}
