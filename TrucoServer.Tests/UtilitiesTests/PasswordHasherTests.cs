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
        public void TestHashReturnsNonEmptyString()
        {
            string password = "MySecurePassword";
            string hash = PasswordHasher.Hash(password);
            Assert.IsFalse(string.IsNullOrEmpty(hash));
        }

        [TestMethod]
        public void TestVerifyReturnsTrueForCorrectPassword()
        {
            string password = "Password123";
            string hash = PasswordHasher.Hash(password);
            bool result = PasswordHasher.Verify(password, hash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestVerifyReturnsFalseForIncorrectPassword()
        {
            string password = "Password123";
            string wrongPassword = "Password124";
            string hash = PasswordHasher.Hash(password);
            bool result = PasswordHasher.Verify(wrongPassword, hash);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestHashGeneratesDifferentHashesForSamePassword()
        {
            string password = "SamePassword";
            string hash1 = PasswordHasher.Hash(password);
            string hash2 = PasswordHasher.Hash(password);
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void TestVerifyThrowExceptionOnInvalidBase64Hash()
        {
            string password = "Password";
            string invalidHash = "NotABase64String!!";
            bool threwException = false;

            try
            {
                PasswordHasher.Verify(password, invalidHash);
            }
            catch (FormatException)
            {
                threwException = true;
            }

            Assert.IsTrue(threwException);
        }
    }
}