using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class UserProfileDataTests
    {
        [TestMethod]
        public void TestUserProfileDataSetSocialLinksJsonShouldStoreBytes()
        {
            var profile = new UserProfileData();

            byte[] data = new byte[]
            {
                1,
                2,
                3
            };

            profile.SocialLinksJson = data;
            CollectionAssert.AreEqual(data, profile.SocialLinksJson);
        }

        [TestMethod]
        public void TestUserProfileDataDefaultBoolShouldBeFalse()
        {
            var profile = new UserProfileData();
            bool isMuted = profile.IsMusicMuted;
            Assert.IsFalse(isMuted);
        }
    }
}