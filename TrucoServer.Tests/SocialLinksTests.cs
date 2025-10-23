using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class SocialLinksTests
    {
        private SocialLinks GetSampleSocialLinks()
        {
            return new SocialLinks
            {
                FacebookHandle = "testFB",
                XHandle = "testX",
                InstagramHandle = "testIG"
            };
        }

        [TestMethod]
        public void SocialLinksFacebookBeTestFB()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testFB", obj.FacebookHandle);
        }

        [TestMethod]
        public void SocialLinksXBeTestX()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testX", obj.XHandle);
        }

        [TestMethod]
        public void SocialLinksInstagramBeTestIG()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testIG", obj.InstagramHandle);
        }
    }
}
