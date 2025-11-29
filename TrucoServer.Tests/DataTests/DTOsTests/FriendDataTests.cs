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
        public void TestSetUsernameShouldRetrieveSameString()
        {
            var friend = new FriendData();
            string expectedUser = "Crosby";
            friend.Username = expectedUser;
            Assert.AreEqual(expectedUser, friend.Username);
        }

        [TestMethod]
        public void TestEmptyAvatarIdShouldBeAllowed()
        {
            var friend = new FriendData();
            friend.AvatarId = "";
            Assert.AreEqual("", friend.AvatarId);
        }

        [TestMethod]
        public void TestNullUsernameShouldBeNull()
        {
            var friend = new FriendData();
            friend.Username = null;
            Assert.IsNull(friend.Username);
        }

        [TestMethod]
        public void TestSpecialCharactersShouldPersist()
        {
            var friend = new FriendData();
            string trickyName = "Ñoño@#123";
            friend.Username = trickyName;
            Assert.AreEqual(trickyName, friend.Username);
        }

        [TestMethod]
        public void TestNewInstancePropertiesShouldBeNullByDefault()
        {
            var friend = new FriendData();
            var avatar = friend.AvatarId;
            Assert.IsNull(avatar);
        }
    }
}
