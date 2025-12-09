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
    public class ConfigurationFileWrapperTests
    {
        private const int SMTP_PORT= 25;

        [TestMethod]
        public void TestEmailSettingsSetObjectReturnsObject()
        {
            var wrapper = new ConfigurationFileWrapper();
            var emailSettings = new EmailSettings();
            wrapper.EmailSettings = emailSettings;
            Assert.AreSame(emailSettings, wrapper.EmailSettings);
        }

        [TestMethod]
        public void TestEmailSettingsSetNullReturnsNull()
        {
            var wrapper = new ConfigurationFileWrapper();
            wrapper.EmailSettings = null;
            Assert.IsNull(wrapper.EmailSettings);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var wrapper = new ConfigurationFileWrapper();
            Assert.IsNotNull(wrapper);
        }

        [TestMethod]
        public void TestEmailSettingsPropertyCanBeAccessed()
        {
            var wrapper = new ConfigurationFileWrapper();

            wrapper.EmailSettings = new EmailSettings 
            { 
                SmtpPort = SMTP_PORT 
            };

            int port = wrapper.EmailSettings.SmtpPort;
            Assert.AreEqual(25, port);
        }

        [TestMethod]
        public void TestEmailSettingsReassignmentUpdatesReference()
        {
            var wrapper = new ConfigurationFileWrapper();
            var settingsA = new EmailSettings();
            var settingsB = new EmailSettings();
            wrapper.EmailSettings = settingsA;
            wrapper.EmailSettings = settingsB;
            Assert.AreSame(settingsB, wrapper.EmailSettings);
        }
    }
}
