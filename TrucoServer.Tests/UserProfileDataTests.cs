using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class UserProfileDataTests
    {
        private const string TEST_USERNAME = "DeMarius";
        private const string TEST_EMBLEM = "#000000";
        private const int TEST_NAME_CHANGE_COUNT = 5;
        private const int TEST_SHAPE_ID = 10;

        [TestMethod]
        public void TestUsernameSetReturnsCorrectString()
        {
            var userProfile = new UserProfileData();
            string expectedUsername = TEST_USERNAME;

            userProfile.Username = expectedUsername;

            Assert.AreEqual(expectedUsername, userProfile.Username);
        }

        [TestMethod]
        public void TestNameChangeCountSetReturnsCorrectInt()
        {
            var userProfile = new UserProfileData();
            int expectedCount = TEST_NAME_CHANGE_COUNT;

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
            int expectedShapeId = TEST_SHAPE_ID;

            layer.ShapeId = expectedShapeId;

            Assert.AreEqual(expectedShapeId, layer.ShapeId);
        }

        [TestMethod]
        public void TestEmblemLayerColorHexSetReturnsCorrectValue()
        {
            var layer = new EmblemLayer();
            string expectedColor = TEST_EMBLEM;

            layer.ColorHex = expectedColor;

            Assert.AreEqual(expectedColor, layer.ColorHex);
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