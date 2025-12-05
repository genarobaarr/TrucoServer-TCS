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
    public class UsernameUpdateContextTests
    {
        [TestMethod]
        public void TestNewUsernameSetValidStringReturnsString()
        {
            var context = new UsernameUpdateContext();
            string newName = "NewName2025";
            context.NewUsername = newName;
            Assert.AreEqual(newName, context.NewUsername);
        }

        [TestMethod]
        public void TestMaxNameChangesSetPositiveValueReturnsValue()
        {
            var context = new UsernameUpdateContext();
            int max = 3;
            context.MaxNameChanges = max;
            Assert.AreEqual(max, context.MaxNameChanges);
        }
    }
}
