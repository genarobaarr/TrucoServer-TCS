using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class SocialLinksTests
    {
        [TestMethod]
        public void TestSerializeObjectFacebookHandleShouldMapToLowerCaseFacebookKey()
        {
            var links = new SocialLinks { FacebookHandle = "my_fb_user" };
            string json = JsonConvert.SerializeObject(links);
            Assert.IsTrue(json.Contains("\"facebook\":\"my_fb_user\""));
        }

        [TestMethod]
        public void TestDeserializeObjectXKeyShouldMapToXHandleProperty()
        {
            string jsonInput = "{\"x\": \"twitter_user\"}";
            var result = JsonConvert.DeserializeObject<SocialLinks>(jsonInput);
            Assert.AreEqual("twitter_user", result.XHandle);
        }

        [TestMethod]
        public void TestInstagramHandleSetPropertyShouldStoreValue()
        {
            var links = new SocialLinks();
            string expectedHandle = "insta_gram";
            links.InstagramHandle = expectedHandle;
            Assert.AreEqual(expectedHandle, links.InstagramHandle);
        }

        [TestMethod]
        public void TestSerializeObjectNullValuesShouldNotThrowException()
        {
            var links = new SocialLinks { FacebookHandle = null };
            string jsonOutput = JsonConvert.SerializeObject(links);
            Assert.IsNotNull(jsonOutput);
        }

        [TestMethod]
        public void TestInstagramHandleEmptyStringShouldStoreEmptyValue()
        {
            var links = new SocialLinks();
            links.InstagramHandle = string.Empty;
            Assert.AreEqual(string.Empty, links.InstagramHandle);
        }
    }
}
