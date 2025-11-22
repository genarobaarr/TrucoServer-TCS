using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerInformationTests
    {
        private const string TEST_USER_NAME = "User";
        private const string TEST_SECOND_USER_NAME = "UserTest";
        private const string TEST_TEAM_1 = "Team 1";
        private const string TEST_TEAM_2 = "Team 2";
        private const int TEST_EXPECTED_ID = 10;
        private const int TEST_SECOND_ID = 1;
        private const string TEST_OWNER_REFERENCE = "OwnerRef";
        private const string TEST_AVATAR_ID = "avatar_aaa_default";

        [TestMethod]
        public void TestConstructorSetsPlayerIdCorrectly()
        {
            int expectedId = TEST_EXPECTED_ID;

            var player = new PlayerInformation(expectedId, TEST_USER_NAME, TEST_TEAM_1);

            Assert.AreEqual(expectedId, player.PlayerID);
        }

        [TestMethod]
        public void TestConstructorSetsUsernameCorrectly()
        {
            string expectedName = TEST_SECOND_USER_NAME;

            var player = new PlayerInformation(TEST_SECOND_ID, expectedName, TEST_TEAM_1);

            Assert.AreEqual(expectedName, player.Username);
        }

        [TestMethod]
        public void TestConstructorSetsTeamCorrectly()
        {
            string expectedTeam = TEST_TEAM_2;

            var player = new PlayerInformation(TEST_SECOND_ID, TEST_USER_NAME, expectedTeam);

            Assert.AreEqual(expectedTeam, player.Team);
        }

        [TestMethod]
        public void TestConstructorInitializesHandNotNull()
        {
            var player = new PlayerInformation(TEST_SECOND_ID, TEST_USER_NAME, TEST_TEAM_1);

            Assert.IsNotNull(player.Hand);
        }

        [TestMethod]
        public void TestConstructorInitializesHandEmpty()
        {
            var player = new PlayerInformation(TEST_SECOND_ID, TEST_USER_NAME, TEST_TEAM_1);

            Assert.AreEqual(0, player.Hand.Count);
        }

        [TestMethod]
        public void TestAvatarIdSetReturnsCorrectString()
        {
            var player = new PlayerInformation(TEST_SECOND_ID, TEST_USER_NAME, TEST_TEAM_1);
            string expectedAvatar = TEST_AVATAR_ID;

            player.AvatarId = expectedAvatar;

            Assert.AreEqual(expectedAvatar, player.AvatarId);
        }

        [TestMethod]
        public void TestOwnerUsernameSetReturnsCorrectString()
        {
            var player = new PlayerInformation(TEST_SECOND_ID, TEST_USER_NAME, TEST_TEAM_1);
            string expectedOwner = TEST_OWNER_REFERENCE;

            player.OwnerUsername = expectedOwner;

            Assert.AreEqual(expectedOwner, player.OwnerUsername);
        }
    }
}