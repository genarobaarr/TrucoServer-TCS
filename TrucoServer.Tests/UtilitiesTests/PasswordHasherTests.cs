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
    public class PasswordHasherTests
    {
        [TestMethod]
        public void TestHashValidStringShouldReturnBase64String()
        {
            string hash = PasswordHasher.Hash("password123");
            Assert.IsNotNull(hash);
            Assert.IsTrue(hash.Length > 0);
        }

        [TestMethod]
        public void TestHashDifferentSaltsShouldProduceDifferentHashes()
        {
            string hash1 = PasswordHasher.Hash("samePassword");
            string hash2 = PasswordHasher.Hash("samePassword");
            Assert.AreNotEqual(hash1, hash2, "Hashing same password twice should produce different outputs due to random salt.");
        }

        [TestMethod]
        public void TestVerifyCorrectPasswordShouldReturnTrue()
        {
            string password = "mySecretPassword";
            string hash = PasswordHasher.Hash(password);
            bool result = PasswordHasher.Verify(password, hash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestVerifyIncorrectPasswordShouldReturnFalse()
        {
            string password = "mySecretPassword";
            string hash = PasswordHasher.Hash(password);
            bool result = PasswordHasher.Verify("wrongPassword", hash);
            Assert.IsFalse(result);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestVerifyInvalidHashFormatShouldThrowException()
        {
            PasswordHasher.Verify("pass", "NotABase64String");
        }
    }
}