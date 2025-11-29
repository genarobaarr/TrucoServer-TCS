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
        [TestMethod]
        public void TestEmailSettingsPropertySetObjectShouldStoreReference()
        {
            var wrapper = new ConfigurationFileWrapper();
            var settings = new EmailSettings 
            { 
                SmtpHost = "smtp.gmail.com" 
            };

            wrapper.EmailSettings = settings;
            Assert.AreSame(settings, wrapper.EmailSettings);
        }

        [TestMethod]
        public void TestConstructorInitializationShouldHaveNullEmailSettings()
        {
            var wrapper = new ConfigurationFileWrapper();
            Assert.IsNull(wrapper.EmailSettings);
        }
    }
}
