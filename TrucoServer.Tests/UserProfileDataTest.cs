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
        public void UserProfileData_Username_Rafica()
        {
            var userProfile = GetSampleUserProfile();
            Assert.AreEqual("Rafica", userProfile.Username);
        }

        [TestMethod]
        public void UserProfileData_Email_MailExample()
        {
            var userProfile = GetSampleUserProfile();
            Assert.AreEqual("mail@example.com", userProfile.Email);
        }

        [TestMethod]
        public void EmblemLayer_ShapeId_Be1()
        {
            var userProfile = GetSampleUserProfile();
            var firstLayer = userProfile.EmblemLayers[0];
            Assert.AreEqual(1, firstLayer.ShapeId);
        }

        [TestMethod]
        public void EmblemLayer_ColorHex_BeRed()
        {
            var userProfile = GetSampleUserProfile();
            var firstLayer = userProfile.EmblemLayers[0];
            Assert.AreEqual("#FF0000", firstLayer.ColorHex);
        }

        [TestMethod]
        public void EmblemLayer_ZIndex_Be5()
        {
            var userProfile = GetSampleUserProfile();
            var firstLayer = userProfile.EmblemLayers[0];
            Assert.AreEqual(5, firstLayer.ZIndex);
        }

        [TestMethod]
        public void UserProfileData_SocialLinksJsonLength_Be2()
        {
            var userProfile = GetSampleUserProfile();
            Assert.AreEqual(2, userProfile.SocialLinksJson.Length);
        }
    }
}
