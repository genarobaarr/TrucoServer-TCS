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
        public void SocialLinksSerialization_Facebook_Match()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.FacebookHandle, copy.FacebookHandle);
        }

        [TestMethod]
        public void SocialLinksSerialization_X_Match()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.XHandle, copy.XHandle);
        }

        [TestMethod]
        public void SocialLinksSerialization_Instagram_Match()
        {
            var original = GetSampleSocialLinksS();
            string json = JsonConvert.SerializeObject(original);
            var copy = JsonConvert.DeserializeObject<SocialLinks>(json);
            Assert.AreEqual(original.InstagramHandle, copy.InstagramHandle);
        }
    }
}
