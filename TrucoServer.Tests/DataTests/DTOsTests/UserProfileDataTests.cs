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
    public class UserProfileDataTests
    {
        [TestMethod]
        public void TestUsernameSetValidStringReturnsString()
        {
            var profile = new UserProfileData();
            string username = "Gamer123";
            profile.Username = username;
            Assert.AreEqual(username, profile.Username);
        }

        [TestMethod]
        public void TestNameChangeCountSetPositiveValueReturnsValue()
        {
            var profile = new UserProfileData();
            int count = 5;
            profile.NameChangeCount = count;
            Assert.AreEqual(count, profile.NameChangeCount);
        }
    }
}