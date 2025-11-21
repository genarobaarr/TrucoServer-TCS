using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class EmmailSettingsSTests
    {
        private const string TEST_EMAIL_ADDRESS = "test@test.com";
        private const int TEST_EMPTY_STREAM_LENGTH = 0;
        private const int TEST_SMTP_PORT = 587;

        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var settings = new EmailSettings { FromAddress = TEST_EMAIL_ADDRESS };
            var serializer = new DataContractJsonSerializer(typeof(EmailSettings));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, settings);

                Assert.IsTrue(stream.Length > TEST_EMPTY_STREAM_LENGTH);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectFromAddress()
        {
            string json = "{\"FromAddress\":\"test@test.com\",\"SmtpPort\":587}";
            var serializer = new DataContractJsonSerializer(typeof(EmailSettings));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (EmailSettings)serializer.ReadObject(stream);

                Assert.AreEqual(TEST_EMAIL_ADDRESS, result.FromAddress);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectSmtpPort()
        {
            string json = "{\"FromAddress\":\"test@test.com\",\"SmtpPort\":587}";
            var serializer = new DataContractJsonSerializer(typeof(EmailSettings));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (EmailSettings)serializer.ReadObject(stream);

                Assert.AreEqual(TEST_SMTP_PORT, result.SmtpPort);
            }
        }
    }
}