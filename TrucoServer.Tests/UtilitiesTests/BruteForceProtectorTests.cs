using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Security;

namespace TrucoServer.Tests.UtilitiesTests
{
    [TestClass]
    public class BruteForceProtectorTests
    {
        [TestMethod]
        public void TestIsBlockedReturnsFalseForNewUser()
        {
            string user = "NewUser" + Guid.NewGuid();
            bool isBlocked = BruteForceProtector.IsBlocked(user);
            Assert.IsFalse(isBlocked);
        }

        [TestMethod]
        public void TestRegisterFailedAttemptIncrementsCountButDoesNotBlockImmediately()
        {
            string user = "UserAttempt" + Guid.NewGuid();
            BruteForceProtector.RegisterFailedAttempt(user);
            bool isBlocked = BruteForceProtector.IsBlocked(user);
            Assert.IsFalse(isBlocked);
        }

        [TestMethod]
        public void TestIsBlockedReturnsTrueAfterMaxAttempts()
        {
            string user = "UserMax" + Guid.NewGuid();

            for (int i = 0; i < 5; i++)
            {
                BruteForceProtector.RegisterFailedAttempt(user);
            }

            bool isBlocked = BruteForceProtector.IsBlocked(user);
            Assert.IsTrue(isBlocked);
        }

        [TestMethod]
        public void TestRegisterSuccessClearsBlockOrAttempts()
        {
            string user = "UserClear" + Guid.NewGuid();
            BruteForceProtector.RegisterFailedAttempt(user);
            BruteForceProtector.RegisterSuccess(user);
            bool isBlocked = BruteForceProtector.IsBlocked(user);
            Assert.IsFalse(isBlocked);
        }

        [TestMethod]
        public void TestIsBlockedReturnsFalseForNullIdentifier()
        {
            string user = null;
            bool result = BruteForceProtector.IsBlocked(user);
            Assert.IsFalse(result);
        }
    }
}
