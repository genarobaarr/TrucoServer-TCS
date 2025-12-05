using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.UtilitiesTests
{
    [TestClass]
    public class EmailSettingsTests
    {
        [TestMethod]
        public void TestFromAddressSetValidStringReturnsString()
        {
            var settings = new EmailSettings();
            string email = "noreply@gmail.com";
            settings.FromAddress = email;
            Assert.AreEqual(email, settings.FromAddress);
        }

        [TestMethod]
        public void TestSmtpPortSetIntegerReturnsInteger()
        {
            var settings = new EmailSettings();
            int port = 587;
            settings.SmtpPort = port;
            Assert.AreEqual(port, settings.SmtpPort);
        }

        [TestMethod]
        public void TestEnableSslSetTrueReturnsTrue()
        {
            var settings = new EmailSettings();
            settings.EnableSsl = true;
            Assert.IsTrue(settings.EnableSsl);
        }

        [TestMethod]
        public void TestFromPasswordSetStringReturnsString()
        {
            var settings = new EmailSettings();
            string pass = "Secret123";
            settings.FromPassword = pass;
            Assert.AreEqual(pass, settings.FromPassword);
        }
    }
}
