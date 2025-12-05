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
        public void TestErrorMessageSetStringReturnsString()
        {
            var fault = new LoginFault();
            string msg = "Invalid Password";
            fault.ErrorMessage = msg;
            Assert.AreEqual(msg, fault.ErrorMessage);
        }

        [TestMethod]
        public void TestErrorCodeSetStringReturnsString()
        {
            var fault = new LoginFault();
            string code = "AUMA001";
            fault.ErrorCode = code;
            Assert.AreEqual(code, fault.ErrorCode);
        }

        [TestMethod]
        public void TestErrorMessageSetNullReturnsNull()
        {
            var fault = new LoginFault();
            fault.ErrorMessage = null;
            Assert.IsNull(fault.ErrorMessage);
        }

        [TestMethod]
        public void TestErrorCodeSetEmptyReturnsEmpty()
        {
            var fault = new LoginFault();
            fault.ErrorCode = string.Empty;
            Assert.AreEqual(string.Empty, fault.ErrorCode);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var fault = new LoginFault();
            Assert.IsNotNull(fault);
        }
    }
}
