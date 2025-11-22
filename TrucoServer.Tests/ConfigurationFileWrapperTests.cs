using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Utilities;

namespace TrucoServer.Tests
{
    [TestClass]
    public class ConfigurationFileWrapperTests
    {
        [TestMethod]
        public void TestEmailSettingsPropertyInitializeReturnsNull()
        {
            var wrapper = new ConfigurationFileWrapper();

            Assert.IsNull(wrapper.EmailSettings);
        }

        [TestMethod]
        public void TestEmailSettingsPropertySetReturnsCorrectObject()
        {
            var wrapper = new ConfigurationFileWrapper();
            var settings = new EmailSettings();

            wrapper.EmailSettings = settings;

            Assert.AreEqual(settings, wrapper.EmailSettings);
        }
    }
}