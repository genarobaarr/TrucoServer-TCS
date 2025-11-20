using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class UserProfileDataTests
    {
        [TestMethod]
        public void TestUsernameSetReturnsCorrectString()
        {
            var userProfile = new UserProfileData();
            string expectedUsername = "PlayerOne";

            userProfile.Username = expectedUsername;

            Assert.AreEqual(expectedUsername, userProfile.Username);
        }

        [TestMethod]
        public void TestNameChangeCountSetReturnsCorrectInt()
        {
            var userProfile = new UserProfileData();
            int expectedCount = 5;

            userProfile.NameChangeCount = expectedCount;

            Assert.AreEqual(expectedCount, userProfile.NameChangeCount);
        }

        [TestMethod]
        public void TestEmblemLayersInitializeReturnsNotNull()
        {
            var userProfile = new UserProfileData();
            var layers = new List<EmblemLayer>();

            userProfile.EmblemLayers = layers;

            Assert.IsNotNull(userProfile.EmblemLayers);
        }

        [TestMethod]
        public void TestEmblemLayerShapeIdSetReturnsCorrectValue()
        {
            var layer = new EmblemLayer();
            int expectedShapeId = 10;

            layer.ShapeId = expectedShapeId;

            Assert.AreEqual(expectedShapeId, layer.ShapeId);
        }

        [TestMethod]
        public void TestEmblemLayerColorHexSetReturnsCorrectValue()
        {
            var layer = new EmblemLayer();
            string expectedColor = "#FFFFFF";

            layer.ColorHex = expectedColor;

            Assert.AreEqual(expectedColor, layer.ColorHex);
        }

        [TestMethod]
        public void TestEmblemLayerXSetReturnsCorrectValue()
        {
            var layer = new EmblemLayer();
            double expectedX = 15.5;

            layer.X = expectedX;

            Assert.AreEqual(expectedX, layer.X);
        }

        [TestMethod]
        public void TestNameChangeCountSetBoundaryValueReturnsCorrectValue()
        {
            var userProfile = new UserProfileData();

            userProfile.NameChangeCount = int.MaxValue;

            Assert.AreEqual(int.MaxValue, userProfile.NameChangeCount);
        }

        [TestMethod]
        public void TestEmailSetNullReturnsNull()
        {
            var userProfile = new UserProfileData();

            userProfile.Email = null;

            Assert.IsNull(userProfile.Email);
        }

        [TestMethod]
        public void TestSocialLinksJsonSetNullReturnsNull()
        {
            var userProfile = new UserProfileData();

            userProfile.SocialLinksJson = null;

            Assert.IsNull(userProfile.SocialLinksJson);
        }
    }
}