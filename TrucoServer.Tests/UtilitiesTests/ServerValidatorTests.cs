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
        public void TestIsUsernameValidReturnsTrueForAlphanumericString()
        {
            string validUsername = "Gamer123";
            bool result = ServerValidator.IsUsernameValid(validUsername);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsUsernameValidReturnsTrueForUsernameWithUnderscore()
        {
            string validUsername = "Gamer_123";
            bool result = ServerValidator.IsUsernameValid(validUsername);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsUsernameValidReturnsFalseForEmptyString()
        {
            string empty = string.Empty;
            bool result = ServerValidator.IsUsernameValid(empty);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsUsernameValidReturnsFalseForStringExceedingLength()
        {
            string longName = "ThisUsernameIsWayTooLongTo beValid123";
            bool result = ServerValidator.IsUsernameValid(longName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsUsernameValidReturnsFalseForInvalidCharacters()
        {
            string invalidName = "User@Name";
            bool result = ServerValidator.IsUsernameValid(invalidName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsEmailValidReturnsTrueForStandardFormat()
        {
            string validEmail = "username@gmail.com";
            bool result = ServerValidator.IsEmailValid(validEmail);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsEmailValidReturnsFalseForMissingAtSymbol()
        {
            string invalidEmail = "username.domain.com";
            bool result = ServerValidator.IsEmailValid(invalidEmail);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsPasswordValidReturnsTrueForComplexPassword()
        {
            string validPass = "SecurePass123!";
            bool result = ServerValidator.IsPasswordValid(validPass);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsPasswordValidReturnsFalseForShortPassword()
        {
            string shortPass = "Short1!";
            bool result = ServerValidator.IsPasswordValid(shortPass);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsPasswordValidReturnsFalseForPasswordWithoutSpecialChar()
        {
            string noSpecial = "SecurePass123";
            bool result = ServerValidator.IsPasswordValid(noSpecial);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsPasswordValidReturnsFalseForPasswordWithoutDigits()
        {
            string noDigit = "SecurePass!";
            bool result = ServerValidator.IsPasswordValid(noDigit);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsMatchCodeValidReturnsTrueForSixUppercaseAlphanumerics()
        {
            string validCode = "ABC123";
            bool result = ServerValidator.IsMatchCodeValid(validCode);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsMatchCodeValidReturnsFalseForLowerCaseCharacters()
        {
            string lowerCode = "abc123";
            bool result = ServerValidator.IsMatchCodeValid(lowerCode);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsMatchCodeValidReturnsFalseForInvalidLength()
        {
            string shortCode = "ABC";
            bool result = ServerValidator.IsMatchCodeValid(shortCode);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsIdValidReturnsTrueForPositiveIntegerString()
        {
            string validId = "100";
            bool result = ServerValidator.IsIdValid(validId);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsIdValidReturnsFalseForNonNumericString()
        {
            string notNumber = "ABC";
            bool result = ServerValidator.IsIdValid(notNumber);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsIdValidReturnsFalseForZeroOrNegative()
        {
            string zeroId = "0";
            bool result = ServerValidator.IsIdValid(zeroId);
            Assert.IsFalse(result);
        }
    }
}
