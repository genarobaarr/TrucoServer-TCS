using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Mapping;

namespace TrucoServer.Tests.HelpersTests.MappingTests
{
    [TestClass]
    public class UserMapperTests
    {
        [TestMethod]
        public void TestMapUserToProfileDataHandlesNullUserProfile()
        {
            var mapper = new UserMapper();
            
            var user = new User 
            {
                username = "Test",
                email = "test@gmail.com",
                UserProfile = null 
            };

            var result = mapper.MapUserToProfileData(user);
            Assert.AreEqual("avatar_aaa_default", result.AvatarId);
        }

        [TestMethod]
        public void TestMapUserToProfileDataHandlesCorruptJson()
        {
            var mapper = new UserMapper();
            
            var user = new User
            {
                username = "Test",
                UserProfile = new UserProfile
                {
                    socialLinksJson = Encoding.UTF8.GetBytes("{InvalidJson")
                }
            };

            Assert.ThrowsException<JsonReaderException>(() => mapper.MapUserToProfileData(user));
        }

        [TestMethod]
        public void TestMapUserToProfileDataMapsAllFieldsCorrectly()
        {
            var mapper = new UserMapper();
            string json = "{\"facebook\":\"fb\",\"x\":\"xx\",\"instagram\":\"ig\"}";
            
            var user = new User
            {
                username = "User",
                email = "test@gmail.com",
                nameChangeCount = 5,
                UserProfile = new UserProfile
                {
                    avatarID = "avatar_aaa_default",
                    languageCode = "en-US",
                    isMusicMuted = true,
                    socialLinksJson = Encoding.UTF8.GetBytes(json)
                }
            };

            var result = mapper.MapUserToProfileData(user);
            Assert.AreEqual("fb", result.FacebookHandle);
        }

        [TestMethod]
        public void TestMapUserToProfileDataHandlesNullSocialLinksJson()
        {
            var mapper = new UserMapper();
           
            var user = new User
            {
                UserProfile = new UserProfile 
                {
                    socialLinksJson = null
                }
            };

            var result = mapper.MapUserToProfileData(user);
            Assert.AreEqual(string.Empty, result.FacebookHandle);
        }

        [TestMethod]
        public void TestMapUserToProfileDataUsesDefaultsForNullFields()
        {
            var mapper = new UserMapper();
          
            var user = new User
            {
                UserProfile = new UserProfile
                { 
                    languageCode = null, 
                    avatarID = null 
                }
            };

            var result = mapper.MapUserToProfileData(user);
            Assert.AreEqual("es-MX", result.LanguageCode);
        }
    }
}
