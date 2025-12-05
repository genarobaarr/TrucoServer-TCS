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
    public class UserLookupOptionsTests
    {
        [TestMethod]
        public void TestUsername1SetStringReturnsString()
        {
            var options = new UserLookupOptions();
            string user = "UserOne";
            options.Username1 = user;
            Assert.AreEqual(user, options.Username1);
        }

        [TestMethod]
        public void TestUsername2SetStringReturnsString()
        {
            var options = new UserLookupOptions();
            string user = "UserTwo";
            options.Username2 = user;
            Assert.AreEqual(user, options.Username2);
        }

        [TestMethod]
        public void TestUsername1SetNullReturnsNull()
        {
            var options = new UserLookupOptions();
            options.Username1 = null;
            Assert.IsNull(options.Username1);
        }

        [TestMethod]
        public void TestUsername2SetEmptyStringReturnsEmpty()
        {
            var options = new UserLookupOptions();
            options.Username2 = string.Empty;
            Assert.AreEqual(string.Empty, options.Username2);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var options = new UserLookupOptions();
            Assert.IsNotNull(options);
        }
    }
}
