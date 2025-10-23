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
        public void SocialLinks_Facebook_BeTestFB()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testFB", obj.FacebookHandle);
        }

        [TestMethod]
        public void SocialLinks_X_BeTestX()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testX", obj.XHandle);
        }

        [TestMethod]
        public void SocialLinks_Instagram_BeTestIG()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual("testIG", obj.InstagramHandle);
        }
    }
}
