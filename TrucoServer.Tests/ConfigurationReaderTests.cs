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
        private const string TestFilePath = "appSettings.private.json";

        private const string ValidJsonContent = @"
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

        private const string JsonMissingKey = @"{ ""OtherSettings"": { ""Key"": ""Value"" } }";
        private const string InvalidJsonFormat = @"{ ""EmailSettings"": { ""FromAddress"": ""test"" "; 

        [TestCleanup]
        public void TestCleanup()
        {
            Type type = typeof(ConfigurationReader);
            FieldInfo field = type.GetField("emailSettings", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, null);

            if (File.Exists(TestFilePath))
            {
                File.Delete(TestFilePath);
            }
        }

        private void WriteFile(string content)
        {
            File.WriteAllText(TestFilePath, content);
        }

        [TestMethod]
        public void TestEmailSettingsLoadCorrectly()
        {
            WriteFile(ValidJsonContent);
            var settings = ConfigurationReader.EmailSettings;
            Assert.AreEqual("test@truco.com", settings.FromAddress);
        }

        [TestMethod]
        public void TestEmailSettingsReturnsCachedInstance()
        {
            WriteFile(ValidJsonContent);
            var settings1 = ConfigurationReader.EmailSettings;
            var settings2 = ConfigurationReader.EmailSettings;
            Assert.AreSame(settings1, settings2);
        }
    }
}
