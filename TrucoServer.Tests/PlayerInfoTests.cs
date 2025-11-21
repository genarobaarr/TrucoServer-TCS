using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PlayerInfoTests
    {
        private const string TEST_USERNAME = "Makelele";
        private const string TEST_TEAM_1 = "Team 1";
        private const string TEST_AVATAR_ID = "avatar_aaa_default";
        private const string TEST_OWNER_USERNAME = "Raphina";

        [TestMethod]
        public void TestUsernameSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = TEST_USERNAME;

            playerInfo.Username = expected;

            Assert.AreEqual(expected, playerInfo.Username);
        }

        [TestMethod]
        public void TestAvatarIdSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = TEST_AVATAR_ID;

            playerInfo.AvatarId = expected;

            Assert.AreEqual(expected, playerInfo.AvatarId);
        }

        [TestMethod]
        public void TestOwnerUsernameSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = TEST_OWNER_USERNAME;

            playerInfo.OwnerUsername = expected;

            Assert.AreEqual(expected, playerInfo.OwnerUsername);
        }

        [TestMethod]
        public void TestTeamSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = TEST_TEAM_1;

            playerInfo.Team = expected;

            Assert.AreEqual(expected, playerInfo.Team);
        }

        [TestMethod]
        public void TestUsernameSetNullReturnsNull()
        {
            var playerInfo = new PlayerInfo();

            playerInfo.Username = null;

            Assert.IsNull(playerInfo.Username);
        }
    }
}