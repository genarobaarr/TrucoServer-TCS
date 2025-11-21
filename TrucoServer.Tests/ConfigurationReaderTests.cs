using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrucoServer;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ConfigurationReaderTests
    {
        private const string TEST_FILE_PATH = "appSettings.private.json";
        private const string TEST_EMAIL_ADDRESS = "test@truco.com";
        private const string TEST_VALID_JSON_CONTENT = @"
        {
            ""EmailSettings"": {
                ""FromAddress"": ""test@truco.com"",
                ""FromDisplayName"": ""Truco Server"",
                ""FromPassword"": ""password123"",
                ""SmtpHost"": ""smtp.test.com"",
                ""SmtpPort"": 777,
                ""EnableSsl"": true
            }
        }";

        private const string TEST_JSON_MISSING_KEY = @"{ ""OtherSettings"": { ""Key"": ""Value"" } }";
        private const string TEST_INVALID_JSON_FORMAT = @"{ ""EmailSettings"": { ""FromAddress"": ""test"" "; 

        [TestCleanup]
        public void TestCleanup()
        {
            Type type = typeof(ConfigurationReader);
            FieldInfo field = type.GetField(TEST_EMAIL_ADDRESS, BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, null);

            if (File.Exists(TEST_FILE_PATH))
            {
                File.Delete(TEST_FILE_PATH);
            }
        }

        private void WriteFile(string content)
        {
            File.WriteAllText(TEST_FILE_PATH, content);
        }

        [TestMethod]
        public void TestEmailSettingsLoadCorrectly()
        {
            WriteFile(TEST_VALID_JSON_CONTENT);
            var settings = ConfigurationReader.EmailSettings;
            Assert.AreEqual(TEST_EMAIL_ADDRESS, settings.FromAddress);
        }

        [TestMethod]
        public void TestEmailSettingsReturnsCachedInstance()
        {
            WriteFile(TEST_VALID_JSON_CONTENT);
            var settings1 = ConfigurationReader.EmailSettings;
            var settings2 = ConfigurationReader.EmailSettings;
            Assert.AreSame(settings1, settings2);
        }
    }
}
