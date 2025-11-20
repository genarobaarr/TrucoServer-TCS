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
    public class ConfigurationFileWrapperSTests
    {
        [TestMethod]
        public void TestSerializationReturnsNotEmptyStream()
        {
            var wrapper = new ConfigurationFIleWrapper { EmailSettings = new EmailSettings() };
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationFIleWrapper));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, wrapper);

                Assert.IsTrue(stream.Length > 0);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsNotNullEmailSettings()
        {
            string json = "{\"EmailSettings\":{\"FromAddress\":\"test@test.com\"}}";
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationFIleWrapper));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (ConfigurationFIleWrapper)serializer.ReadObject(stream);

                Assert.IsNotNull(result.EmailSettings);
            }
        }

        [TestMethod]
        public void TestDeserializationReturnsCorrectInnerValue()
        {
            string json = "{\"EmailSettings\":{\"FromAddress\":\"test@test.com\"}}";
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationFIleWrapper));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (ConfigurationFIleWrapper)serializer.ReadObject(stream);

                Assert.AreEqual("test@test.com", result.EmailSettings.FromAddress);
            }
        }
    }
}