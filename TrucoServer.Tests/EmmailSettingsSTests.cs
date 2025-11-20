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
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var settings = new EmailSettings { FromAddress = "test@test.com" };
            var serializer = new DataContractJsonSerializer(typeof(EmailSettings));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, settings);

                Assert.IsTrue(stream.Length > 0);
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

                Assert.AreEqual("test@test.com", result.FromAddress);
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

                Assert.AreEqual(587, result.SmtpPort);
            }
        }
    }
}