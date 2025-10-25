using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class UserProfileDataTest
    {
        private UserProfileData GetSampleUserProfile()
        {
            var emblemLayer = new EmblemLayer
            {
                ShapeId = 1,
                ColorHex = "#FF0000",
                X = 10,
                Y = 20,
                ScaleX = 1.2,
                ScaleY = 1.2,
                Rotation = 45,
                ZIndex = 5
            };

            return new UserProfileData
            {
                Username = "Rafica",
                Email = "mail@example.com",
                AvatarId = "avatar_aaa_default",
                NameChangeCount = 2,
                FacebookHandle = "raficaFB",
                XHandle = "raficaX",
                InstagramHandle = "raficaIG",
                EmblemLayers = new List<EmblemLayer> { emblemLayer },
                SocialLinksJson = new byte[] { 0x01, 0x02 }
            };
        }

        [TestMethod]
        public void TestUserProfileDataUsernameBeRafica()
        {
            var userProfile = GetSampleUserProfile();
            Assert.AreEqual("Rafica", userProfile.Username);
        }

        [TestMethod]
        public void TestUserProfileDataEmailBeMailExample()
        {
            var userProfile = GetSampleUserProfile();
            Assert.AreEqual("mail@example.com", userProfile.Email);
        }

        [TestMethod]
        public void TestEmblemLayerShapeIdBe1()
        {
            var userProfile = GetSampleUserProfile();
            var firstLayer = userProfile.EmblemLayers[0];
            Assert.AreEqual(1, firstLayer.ShapeId);
        }

        [TestMethod]
        public void TestEmblemLayerColorHexBeRed()
        {
            var userProfile = GetSampleUserProfile();
            var firstLayer = userProfile.EmblemLayers[0];
            Assert.AreEqual("#FF0000", firstLayer.ColorHex);
        }

        [TestMethod]
        public void TestEmblemLayerZIndexBe5()
        {
            var userProfile = GetSampleUserProfile();
            var firstLayer = userProfile.EmblemLayers[0];
            Assert.AreEqual(5, firstLayer.ZIndex);
        }

        [TestMethod]
        public void TestUserProfileDataSocialLinksJsonLengthBe2()
        {
            var userProfile = GetSampleUserProfile();
            Assert.AreEqual(2, userProfile.SocialLinksJson.Length);
        }
    }
}
