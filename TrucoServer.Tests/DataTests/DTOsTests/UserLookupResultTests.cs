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
    public class UserLookupResultTests
    {
        [TestMethod]
        public void TestSuccessSetTrueReturnsTrue()
        {
            var result = new UserLookupResult();
            result.Success = true;
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void TestUser1SetObjectReturnsObject()
        {
            var result = new UserLookupResult();
            var user = new User();
            result.User1 = user;
            Assert.AreSame(user, result.User1);
        }

        [TestMethod]
        public void TestUser2SetObjectReturnsObject()
        {
            var result = new UserLookupResult();
            var user = new User();
            result.User2 = user;
            Assert.AreSame(user, result.User2);
        }
    }
}
