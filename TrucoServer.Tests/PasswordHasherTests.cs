using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrucoServer.Utilities;

namespace TrucoServer.Tests
{
    [TestClass]
    public class PasswordHasherTests
    {
        private const string TEST_PASSWORD = "chimalhuacanGOAT!";
        private const string TEST_MALFORMED_HASH = "ABC123";
        private const string TEST_INCORRECT_PASSWORD = "trestristrestragos";

        [TestMethod]
        public void TestHashReturnDifferentFromPassword()
        {
            string password = TEST_PASSWORD;
            string hashed = PasswordHasher.Hash(password);
            Assert.AreNotEqual(password, hashed, "The hash must not match the original password");
        }

        [TestMethod]
        public void TestHashReturnNullFromPassword()
        {
            string password = TEST_PASSWORD;
            string hashed = PasswordHasher.Hash(password);
            Assert.IsNotNull(hashed, "The hash must not be null");
        }

        [TestMethod]
        public void TestVerifyReturnTrueForCorrectPassword()
        {
            string password = TEST_PASSWORD;
            string hash = PasswordHasher.Hash(password);
            bool isValid = PasswordHasher.Verify(password, hash);
            Assert.IsTrue(isValid, "The verification should be true for the correct password");
        }

        [TestMethod]
        public void TestVerifyReturnFalseForIncorrectPassword()
        {
            string password = TEST_PASSWORD;
            string hash = PasswordHasher.Hash(password);
            bool isValid = PasswordHasher.Verify(TEST_INCORRECT_PASSWORD, hash);

            Assert.IsFalse(isValid, "The verification should be false for the wrong password");
        }

        [TestMethod]
        public void TestHashProduceDifferentHashesForSamePassword()
        {
            string password = TEST_PASSWORD;

            string hash1 = PasswordHasher.Hash(password);
            string hash2 = PasswordHasher.Hash(password);
            Assert.AreNotEqual(hash1, hash2, "Each hash must be unique even for the same password");
        }

        [TestMethod]
        public void TestVerifyReturnFalseForMalformedHash()
        {
            string password = TEST_PASSWORD;
            string malformedHash = TEST_MALFORMED_HASH;
            bool isValid = false;
            try
            {
                isValid = PasswordHasher.Verify(password, malformedHash);
            }
            catch (FormatException)
            {
                isValid = false;
            }
            Assert.IsFalse(isValid, "An incorrectly formatted hash should not be validated");
        }
    }
}
