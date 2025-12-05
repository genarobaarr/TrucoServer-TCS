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
    public class FriendDataTests
    {
        [TestMethod]
        public void TestUsernameSetEmptyReturnsEmpty()
        {
            var data = new FriendData();
            string empty = "";
            data.Username = empty;
            Assert.AreEqual(string.Empty, data.Username);
        }

        [TestMethod]
        public void TestAvatarIdSetStringReturnsString()
        {
            var data = new FriendData();
            string avatar = "avatar_aaa_deault.png";
            data.AvatarId = avatar;
            Assert.AreEqual(avatar, data.AvatarId);
        }

        [TestMethod]
        public void TestUsernameSetNullReturnsNull()
        {
            var data = new FriendData();
            data.Username = null;
            Assert.IsNull(data.Username);
        }

        [TestMethod]
        public void TestAvatarIdSetNumericStringReturnsString()
        {
            var data = new FriendData();
            string numAvatar = "12345";
            data.AvatarId = numAvatar;
            Assert.AreEqual("12345", data.AvatarId);
        }

        [TestMethod]
        public void TestConstructorPropertiesStartNull()
        {
            var data = new FriendData();
            Assert.IsNull(data.Username);
        }
    }
}
