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
        public void TestFromAddressPropertySetShouldStoreValue()
        {
            var settings = new EmailSettings();
            string email = "test@gmail.com";
            settings.FromAddress = email;
            Assert.AreEqual(email, settings.FromAddress);
        }

        [TestMethod]
        public void TestSmtpPortPropertySetShouldStoreIntegerValue()
        {
            var settings = new EmailSettings();
            int port = 587;
            settings.SmtpPort = port;
            Assert.AreEqual(port, settings.SmtpPort);
        }

        [TestMethod]
        public void TestEnableSslPropertySetTrueShouldStoreTrue()
        {
            var settings = new EmailSettings();
            settings.EnableSsl = true;
            Assert.IsTrue(settings.EnableSsl);
        }

        [TestMethod]
        public void TestFromPasswordPropertySetNullShouldStoreNull()
        {
            var settings = new EmailSettings();
            settings.FromPassword = null;
            Assert.IsNull(settings.FromPassword);
        }
    }
}
