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
        public void TestSocialLinksFacebookBeTestFB()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testFB", obj.FacebookHandle);
        }

        [TestMethod]
        public void TestSocialLinksXBeTestX()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testX", obj.XHandle);
        }

        [TestMethod]
        public void TestSocialLinksInstagramBeTestIG()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testIG", obj.InstagramHandle);
        }
    }
}
