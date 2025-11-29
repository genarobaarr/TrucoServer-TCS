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
    public class ServerValidatorTests
    {
        [TestMethod]
        public void TestIsUsernameValidValidAlphanumericShouldReturnTrue()
        {
            Assert.IsTrue(ServerValidator.IsUsernameValid("Player123"));
        }

        [TestMethod]
        public void TestIsUsernameValidWithUnderscoreShouldReturnTrue()
        {
            Assert.IsTrue(ServerValidator.IsUsernameValid("Player_One"));
        }

        [TestMethod]
        public void TestIsUsernameValidTooLongShouldReturnFalse()
        {
            string longName = "ThisUserIsWayTooLong1";
            Assert.IsFalse(ServerValidator.IsUsernameValid(longName));
        }

        [TestMethod]
        public void TestIsUsernameValidSpecialCharsShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsUsernameValid("User@Name!"));
        }

        [TestMethod]
        public void TestIsUsernameValidNullInputShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsUsernameValid(null));
        }

        [TestMethod]
        public void TestIsEmailValidStandardFormatShouldReturnTrue()
        {
            Assert.IsTrue(ServerValidator.IsEmailValid("user@example.com"));
        }

        [TestMethod]
        public void TestIsEmailValidMissingAtSymbolShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsEmailValid("userexample.com"));
        }

        [TestMethod]
        public void TestIsEmailValidMissingDomainExtensionShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsEmailValid("user@example"));
        }

        [TestMethod]
        public void TestIsEmailValidEmptyStringShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsEmailValid(string.Empty));
        }

        [TestMethod]
        public void TestIsPasswordValidComplexPasswordShouldReturnTrue()
        {
            Assert.IsTrue(ServerValidator.IsPasswordValid("Pa$$w0rdStrong!"));
        }

        [TestMethod]
        public void TestIsPasswordValidTooShortShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsPasswordValid("Short1!AaBb"));
        }

        [TestMethod]
        public void TestIsPasswordValidMissingUpperCaseShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsPasswordValid("password123!"));
        }

        [TestMethod]
        public void TestIsPasswordValidMissingDigitShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsPasswordValid("PasswordSafe!"));
        }

        [TestMethod]
        public void TestIsPasswordValidMissingSymbolShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsPasswordValid("Password123456"));
        }

        [TestMethod]
        public void TestIsMatchCodeValidCorrectFormatShouldReturnTrue()
        {
            Assert.IsTrue(ServerValidator.IsMatchCodeValid("ABC123"));
        }

        [TestMethod]
        public void TestIsMatchCodeValidWrongLengthShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsMatchCodeValid("ABC12"));
        }

        [TestMethod]
        public void TestIsMatchCodeValidLowerCaseShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsMatchCodeValid("abc123"));
        }

        [TestMethod]
        public void TestIsIdValidPositiveIntegerStringShouldReturnTrue()
        {
            Assert.IsTrue(ServerValidator.IsIdValid("105"));
        }

        [TestMethod]
        public void TestIsIdValidNonNumericStringShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsIdValid("12a"));
        }

        [TestMethod]
        public void TestIsIdValidZeroShouldReturnFalse()
        {
            Assert.IsFalse(ServerValidator.IsIdValid("0"));
        }
    }
}
