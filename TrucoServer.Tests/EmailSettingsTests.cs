using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Utilities;

namespace TrucoServer.Tests
{
    [TestClass]
    public class EmailSettingsTests
    {
        private const string TEST_ADDRESS = "test@example.com";
        private const string TEST_DISPLAY_NAME = "Truco Test Server";
        private const string TEST_PASSWORD = "a_secure_test_password";
        private const string TEST_HOST = "smtp.test.com";
        private const int TEST_PORT = 465;
        private const bool TEST_SSL = true;

        private EmailSettings GetSampleSettings()
        {
            return new EmailSettings
            {
                FromAddress = TEST_ADDRESS,
                FromDisplayName = TEST_DISPLAY_NAME,
                FromPassword = TEST_PASSWORD,
                SmtpHost = TEST_HOST,
                SmtpPort = TEST_PORT,
                EnableSsl = TEST_SSL
            };
        }

        [TestMethod]
        public void TestEmailSettingsFromAddressIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TEST_ADDRESS, settings.FromAddress);
        }

        [TestMethod]
        public void TestEmailSettingsFromDisplayNameIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TEST_DISPLAY_NAME, settings.FromDisplayName);
        }

        [TestMethod]
        public void TestEmailSettingsFromPasswordIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TEST_PASSWORD, settings.FromPassword);
        }

        [TestMethod]
        public void TestEmailSettingsSmtpHostIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TEST_HOST, settings.SmtpHost);
        }

        [TestMethod]
        public void TestEmailSettingsSmtpPortIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TEST_PORT, settings.SmtpPort);
        }

        [TestMethod]
        public void TestEmailSettingsEnableSslIsAssignedCorrectly()
        {
            var settings = GetSampleSettings();
            Assert.AreEqual(TEST_SSL, settings.EnableSsl);
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

