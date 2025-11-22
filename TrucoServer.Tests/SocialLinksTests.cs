using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class SocialLinksTests
    {
        private const string TEST_FACEBOOK_HANDLE = "testFB";
        private const string TEST_X_HANDLE = "testX";
        private const string TEST_INSTAGRAM_HANDLE = "testIG";

        private SocialLinks GetSampleSocialLinks()
        {
            return new SocialLinks
            {
                FacebookHandle = TEST_FACEBOOK_HANDLE,
                XHandle = TEST_X_HANDLE,
                InstagramHandle = TEST_INSTAGRAM_HANDLE
            };
        }

        [TestMethod]
        public void TestSocialLinksFacebookBeTestFB()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual(TEST_FACEBOOK_HANDLE, obj.FacebookHandle);
        }

        [TestMethod]
        public void TestSocialLinksXBeTestX()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual(TEST_X_HANDLE, obj.XHandle);
        }

        [TestMethod]
        public void TestSocialLinksInstagramBeTestIG()
        {
            var obj = GetSampleSocialLinks();
            Assert.AreEqual(TEST_INSTAGRAM_HANDLE, obj.InstagramHandle);
        }
    }
}
