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
    public class LoginFaultTests
    {
        [TestMethod]
        public void TestSetErrorMessageShouldStoreMessage()
        {
            var fault = new LoginFault();
            string msg = "Incorrect password";
            fault.ErrorMessage = msg;
            Assert.AreEqual(msg, fault.ErrorMessage);
        }

        [TestMethod]
        public void TestEmptyErrorCodeShouldStoreEmpty()
        {
            var fault = new LoginFault();
            fault.ErrorCode = string.Empty;
            Assert.AreEqual(string.Empty, fault.ErrorCode);
        }

        [TestMethod]
        public void TestNullMessageShouldBeAllowed()
        {
            var fault = new LoginFault();
            fault.ErrorMessage = null;
            Assert.IsNull(fault.ErrorMessage);
        }

        [TestMethod]
        public void TestNumericStringCodeShouldPersist()
        {
            var fault = new LoginFault();
            fault.ErrorCode = "404";
            Assert.AreEqual("404", fault.ErrorCode);
        }

        [TestMethod]
        public void TestObjectCreationShouldNotBeNull()
        {
            var fault = new LoginFault();
            Assert.IsNotNull(fault);
        }
    }
}
