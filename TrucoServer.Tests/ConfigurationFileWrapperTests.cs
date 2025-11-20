using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ConfigurationFileWrapperTests
    {
        [TestMethod]
        public void TestEmailSettingsPropertyInitializeReturnsNull()
        {
            var wrapper = new ConfigurationFIleWrapper();

            Assert.IsNull(wrapper.EmailSettings);
        }

        [TestMethod]
        public void TestEmailSettingsPropertySetReturnsCorrectObject()
        {
            var wrapper = new ConfigurationFIleWrapper();
            var settings = new EmailSettings();

            wrapper.EmailSettings = settings;

            Assert.AreEqual(settings, wrapper.EmailSettings);
        }
    }
}