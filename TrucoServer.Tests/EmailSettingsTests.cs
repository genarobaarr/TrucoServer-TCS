using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class EmailSettingsTests
    {
        private const string TestAddress = "test@example.com";
        private const string TestDisplayName = "Truco Test Server";
        private const string TestPassword = "a_secure_test_password";
        private const string TestHost = "smtp.test.com";
        private const int TestPort = 465;
        private const bool TestSsl = true;

        private EmailSettings GetSampleSettings()
        {
            return new EmailSettings
            {
                FromAddress = TestAddress,
                FromDisplayName = TestDisplayName,
                FromPassword = TestPassword,
                SmtpHost = TestHost,
                SmtpPort = TestPort,
                EnableSsl = TestSsl
            };
        }

        [TestMethod]
        public void TestEmailSettingsFromAddressIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TestAddress, settings.FromAddress);
        }

        [TestMethod]
        public void TestEmailSettingsFromDisplayNameIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TestDisplayName, settings.FromDisplayName);
        }

        [TestMethod]
        public void TestEmailSettingsFromPasswordIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TestPassword, settings.FromPassword);
        }

        [TestMethod]
        public void TestEmailSettingsSmtpHostIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TestHost, settings.SmtpHost);
        }

        [TestMethod]
        public void TestEmailSettingsSmtpPortIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TestPort, settings.SmtpPort);
        }

        [TestMethod]
        public void TestEmailSettingsEnableSslIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TestSsl, settings.EnableSsl);
        }

        [TestMethod]
        public void TestEmailSettingsFromAddressIsInitiallyNull()
        {
            EmailSettings settings = new EmailSettings();
            Assert.IsNull(settings.FromAddress);
        }

        [TestMethod]
        public void TestEmailSettingsSmtpPortIsInitiallyZero()
        {
            EmailSettings settings = new EmailSettings();
            Assert.AreEqual(0, settings.SmtpPort);
        }

        [TestMethod]
        public void TestEmailSettingsEnableSslIsInitiallyFalse()
        {
            EmailSettings settings = new EmailSettings();
            Assert.IsFalse(settings.EnableSsl);
        }
    }
}

