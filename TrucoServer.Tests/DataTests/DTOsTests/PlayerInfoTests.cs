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
        public void TestSetOwnerShouldStoreValue()
        {
            var info = new PlayerInfo();
            string owner = "Admin";
            info.OwnerUsername = owner;
            Assert.AreEqual(owner, info.OwnerUsername);
        }

        [TestMethod]
        public void TestNullAvatarShouldBeValid()
        {
            var info = new PlayerInfo();
            info.AvatarId = null;
            Assert.IsNull(info.AvatarId);
        }

        [TestMethod]
        public void TestEmptyTeamShouldStoreEmpty()
        {
            var info = new PlayerInfo();
            info.Team = "";
            Assert.AreEqual(0, info.Team.Length);
        }

        [TestMethod]
        public void TestUpdateUsernameShouldReflectChange()
        {
            var info = new PlayerInfo 
            { 
                Username = "Alexander" 
            };

            info.Username = "Jaire";
            Assert.AreEqual("Jaire", info.Username);
        }

        [TestMethod]
        public void TestInstanceShouldNotBeNull()
        {
            var info = new PlayerInfo();
            Assert.IsNotNull(info);
        }
    }
}
