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
        [TestMethod]
        public void CreateUserProfileDataTest()
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

            var userProfile = new UserProfileData
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
            var firstLayer = userProfile.EmblemLayers[0];

            Assert.AreEqual("Rafica", userProfile.Username);
            Assert.AreEqual("mail@example.com", userProfile.Email);
            Assert.AreEqual(1, firstLayer.ShapeId);
            Assert.AreEqual("#FF0000", firstLayer.ColorHex);
            Assert.AreEqual(5, firstLayer.ZIndex);
            Assert.AreEqual(2, userProfile.SocialLinksJson.Length);
        }
    }
}
