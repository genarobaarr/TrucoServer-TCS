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
    public class PasswordResetOptionsTests
    {
        [TestMethod]
        public void TestCodeSetValidStringReturnsString()
        {
            var options = new PasswordResetOptions();
            string code = "123456";
            options.Code = code;
            Assert.AreEqual(code, options.Code);
        }

        [TestMethod]
        public void TestEmailSetValidStringReturnsString()
        {
            var options = new PasswordResetOptions();
            string email = "user@gmail.com";
            options.Email = email;
            Assert.AreEqual(email, options.Email);
        }

        [TestMethod]
        public void TestNewPasswordSetEmptyStringReturnsEmpty()
        {
            var options = new PasswordResetOptions();
            string emptyPass = "";
            options.NewPassword = emptyPass;
            Assert.AreEqual(string.Empty, options.NewPassword);
        }

        [TestMethod]
        public void TestLanguageCodeSetNullReturnsNull()
        {
            var options = new PasswordResetOptions();
            options.LanguageCode = null;
            Assert.IsNull(options.LanguageCode);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var options = new PasswordResetOptions();
            Assert.IsNotNull(options);
        }
    }
}
