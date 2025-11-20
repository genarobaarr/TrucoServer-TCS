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
        [TestMethod]
        public void TestUsernameSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = "Gamer123";

            playerInfo.Username = expected;

            Assert.AreEqual(expected, playerInfo.Username);
        }

        [TestMethod]
        public void TestAvatarIdSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = "avatar_aaa_default";

            playerInfo.AvatarId = expected;

            Assert.AreEqual(expected, playerInfo.AvatarId);
        }

        [TestMethod]
        public void TestOwnerUsernameSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = "AdminUser";

            playerInfo.OwnerUsername = expected;

            Assert.AreEqual(expected, playerInfo.OwnerUsername);
        }

        [TestMethod]
        public void TestTeamSetReturnsCorrectString()
        {
            var playerInfo = new PlayerInfo();
            string expected = "Team 1";

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