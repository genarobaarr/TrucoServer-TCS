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
        public void TestFacebookHandleSetStringReturnsString()
        {
            var links = new SocialLinks();
            string fb = "user.fb";
            links.FacebookHandle = fb;
            Assert.AreEqual(fb, links.FacebookHandle);
        }

        [TestMethod]
        public void TestXHandleSetStringReturnsString()
        {
            var links = new SocialLinks();
            string x = "@userX";
            links.XHandle = x;
            Assert.AreEqual(x, links.XHandle);
        }

        [TestMethod]
        public void TestInstagramHandleSetStringReturnsString()
        {
            var links = new SocialLinks();
            string insta = "user_insta";
            links.InstagramHandle = insta;
            Assert.AreEqual(insta, links.InstagramHandle);
        }

        [TestMethod]
        public void TestFacebookHandleSetNullReturnsNull()
        {
            var links = new SocialLinks();
            links.FacebookHandle = null;
            Assert.IsNull(links.FacebookHandle);
        }

        [TestMethod]
        public void TestXHandleSetEmptyStringReturnsEmpty()
        {
            var links = new SocialLinks();
            links.XHandle = string.Empty;
            Assert.AreEqual(string.Empty, links.XHandle);
        }
    }
}
