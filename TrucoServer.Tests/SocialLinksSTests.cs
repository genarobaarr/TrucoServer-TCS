using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class SocialLinksSTests
    {
        private const string TEST_FACEBOOK_HANDLE = "testFB";
        private const string TEST_X_HANDLE = "testX";
        private const string TEST_INSTAGRAM_HANDLE = "testIG";

        private SocialLinks GetSampleSocialLinksS()
        {
            return new SocialLinks
            {
                FacebookHandle = TEST_FACEBOOK_HANDLE,
                XHandle = TEST_X_HANDLE,
                InstagramHandle = TEST_INSTAGRAM_HANDLE
            };
        }

        [TestMethod]
        public void TestSocialLinksSerializationFacebookMatch()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.FacebookHandle, copy.FacebookHandle);
        }

        [TestMethod]
        public void TestSocialLinksSerializationXMatch()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.XHandle, copy.XHandle);
        }

        [TestMethod]
        public void TestSocialLinksSerializationInstagramMatch()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.InstagramHandle, copy.InstagramHandle);
        }
    }
}
