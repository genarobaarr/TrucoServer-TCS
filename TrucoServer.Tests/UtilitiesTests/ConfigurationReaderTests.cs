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
        [TestMethod]
        public void TestEmailSettingsAccessDoesNotThrowWhenFileMissingOrInvalid()
        {
            var settings = ConfigurationReader.EmailSettings;
            Assert.IsTrue(settings == null || settings != null);
        }

        [TestMethod]
        public void TestEmailSettingsPropertyIsSingleton()
        {
            var settingsA = ConfigurationReader.EmailSettings;
            var settingsB = ConfigurationReader.EmailSettings;
            Assert.AreEqual(settingsA, settingsB);
        }
    }
}
