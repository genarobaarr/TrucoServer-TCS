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
    public class PlayerInfoTests
    {
        [TestMethod]
        public void TestUsernameSetStringReturnsString()
        {
            var player = new PlayerInfo();
            string name = "PlayerOne";
            player.Username = name;
            Assert.AreEqual(name, player.Username);
        }

        [TestMethod]
        public void TestAvatarIdSetStringReturnsString()
        {
            var player = new PlayerInfo();
            string avatar = "avatar_aaa_default";
            player.AvatarId = avatar;
            Assert.AreEqual(avatar, player.AvatarId);
        }
    }
}
