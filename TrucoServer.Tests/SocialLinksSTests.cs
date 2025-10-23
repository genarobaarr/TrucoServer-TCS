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
        private SocialLinks GetSampleSocialLinksS()
        {
            return new SocialLinks
            {
                FacebookHandle = "testFB",
                XHandle = "testX",
                InstagramHandle = "testIG"
            };
        }

        [TestMethod]
        public void SocialLinksSerializationFacebookMatch()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.FacebookHandle, copy.FacebookHandle);
        }

        [TestMethod]
        public void SocialLinksSerializationXMatch()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.XHandle, copy.XHandle);
        }

        [TestMethod]
        public void SocialLinksSerializationInstagramMatch()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.InstagramHandle, copy.InstagramHandle);
        }
    }
}
