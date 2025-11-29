using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.UtilitiesTests
{
    [TestClass]
    public class ConfigurationReaderTests
    {
        private const string FILE_NAME = "appSettings.private.json";

        [TestInitialize]
        public void Setup()
        {
            var field = typeof(ConfigurationReader).GetField("emailSettings", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
            
            if (File.Exists(FILE_NAME))
            {
                File.Delete(FILE_NAME);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(FILE_NAME)) File.Delete(FILE_NAME);
        }

        [TestMethod]
        public void TestEmailSettingsValidJsonFileShouldLoadProperties()
        {
            string jsonContent = @"{
            'EmailSettings': {
                'FromAddress': 'test@truco.com',
                'SmtpPort': 587
            }
        }";

            File.WriteAllText(FILE_NAME, jsonContent);
            var settings = ConfigurationReader.EmailSettings;
            Assert.IsNotNull(settings);
            Assert.AreEqual("test@truco.com", settings.FromAddress);
            Assert.AreEqual(587, settings.SmtpPort);
        }

        [TestMethod]
        public void TestEmailSettingsMissingFileShouldLogFatalAndNotThrow()
        {
            var settings = ConfigurationReader.EmailSettings;
            Assert.IsNull(settings, "If file is missing, settings should be null as exception is caught.");
        }

        [TestMethod]
        public void TestEmailSettingsMalformedJsonShouldLogFatalAndReturnNull()
        {
            File.WriteAllText(FILE_NAME, "{ Invalid Json }");
            var settings = ConfigurationReader.EmailSettings;
            Assert.IsNull(settings);
        }
    }
}
