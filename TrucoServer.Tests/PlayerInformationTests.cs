using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerInformationTests
    {
        [TestMethod]
        public void TestConstructorSetsPlayerIdCorrectly()
        {
            int expectedId = 10;

            var player = new PlayerInformation(expectedId, "User", "Team 1");

            Assert.AreEqual(expectedId, player.PlayerID);
        }

        [TestMethod]
        public void TestConstructorSetsUsernameCorrectly()
        {
            string expectedName = "UserTest";

            var player = new PlayerInformation(1, expectedName, "Team 1");

            Assert.AreEqual(expectedName, player.Username);
        }

        [TestMethod]
        public void TestConstructorSetsTeamCorrectly()
        {
            string expectedTeam = "Team 2";

            var player = new PlayerInformation(1, "User", expectedTeam);

            Assert.AreEqual(expectedTeam, player.Team);
        }

        [TestMethod]
        public void TestConstructorInitializesHandNotNull()
        {
            var player = new PlayerInformation(1, "User", "Team 1");

            Assert.IsNotNull(player.Hand);
        }

        [TestMethod]
        public void TestConstructorInitializesHandEmpty()
        {
            var player = new PlayerInformation(1, "User", "Team 1");

            Assert.AreEqual(0, player.Hand.Count);
        }

        [TestMethod]
        public void TestAvatarIdSetReturnsCorrectString()
        {
            var player = new PlayerInformation(1, "User", "Team 1");
            string expectedAvatar = "avatar_aaa_default";

            player.AvatarId = expectedAvatar;

            Assert.AreEqual(expectedAvatar, player.AvatarId);
        }

        [TestMethod]
        public void TestOwnerUsernameSetReturnsCorrectString()
        {
            var player = new PlayerInformation(1, "User", "Team 1");
            string expectedOwner = "OwnerRef";

            player.OwnerUsername = expectedOwner;

            Assert.AreEqual(expectedOwner, player.OwnerUsername);
        }
    }
}