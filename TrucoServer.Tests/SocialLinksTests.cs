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
        [TestMethod]
        public void SocialLinksTestTrue()
        {
            var socialLinks = new SocialLinks
            {
                FacebookHandle = "testFB",
                XHandle = "testX",
                InstagramHandle = "testIG"
            };
            Assert.AreEqual("testFB", socialLinks.FacebookHandle);
            Assert.AreEqual("testX", socialLinks.XHandle);
            Assert.AreEqual("testIG", socialLinks.InstagramHandle);
        }
    }
}
