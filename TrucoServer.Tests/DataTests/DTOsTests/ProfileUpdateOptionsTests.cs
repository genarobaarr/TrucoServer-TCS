using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class ProfileUpdateOptionsTests
    {
        [TestMethod]
        public void TestDefaultLanguageCodeSetStringReturnsString()
        {
            var options = new ProfileUpdateOptions();
            string lang = "es-MX";
            options.DefaultLanguageCode = lang;
            Assert.AreEqual(lang, options.DefaultLanguageCode);
        }

        [TestMethod]
        public void TestDefaultAvatarIdSetStringReturnsString()
        {
            var options = new ProfileUpdateOptions();
            string avatar = "avatar_aaa_default";
            options.DefaultAvatarId = avatar;
            Assert.AreEqual(avatar, options.DefaultAvatarId);
        }

        [TestMethod]
        public void TestProfileDataSetNullReturnsNull()
        {
            var options = new ProfileUpdateOptions();
            options.ProfileData = null;
            Assert.IsNull(options.ProfileData);
        }

        [TestMethod]
        public void TestDefaultLanguageCodeSetEmptyReturnsEmpty()
        {
            var options = new ProfileUpdateOptions();
            options.DefaultLanguageCode = string.Empty;
            Assert.AreEqual(string.Empty, options.DefaultLanguageCode);
        }
    }
}
