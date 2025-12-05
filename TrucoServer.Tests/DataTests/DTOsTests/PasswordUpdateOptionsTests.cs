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
    public class PasswordUpdateOptionsTests
    {
        [TestMethod]
        public void TestEmailSetValidStringReturnsString()
        {
            var options = new PasswordUpdateOptions();
            string email = "test@example.com";
            options.Email = email;
            Assert.AreEqual(email, options.Email);
        }

        [TestMethod]
        public void TestNewPasswordSetStringReturnsString()
        {
            var options = new PasswordUpdateOptions();
            string password = "SecurePassword123!";
            options.NewPassword = password;
            Assert.AreEqual(password, options.NewPassword);
        }

        [TestMethod]
        public void TestLanguageCodeSetIsoStringReturnsString()
        {
            var options = new PasswordUpdateOptions();
            string lang = "en-US";
            options.LanguageCode = lang;
            Assert.AreEqual(lang, options.LanguageCode);
        }

        [TestMethod]
        public void TestCallingMethodSetStringReturnsString()
        {
            var options = new PasswordUpdateOptions();
            string method = "GRPC";
            options.CallingMethod = method;
            Assert.AreEqual(method, options.CallingMethod);
        }

        [TestMethod]
        public void TestEmailSetNullReturnsNull()
        {
            var options = new PasswordUpdateOptions();
            options.Email = null;
            Assert.IsNull(options.Email);
        }
    }
}
