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
        [TestInitialize]
        public void Setup()
        {
            var field = typeof(BruteForceProtector).GetField("attempts", BindingFlags.NonPublic | BindingFlags.Static);
            var attempts = (ConcurrentDictionary<string, AttemptInfo>)field.GetValue(null);
            attempts.Clear();
        }

        [TestMethod]
        public void TestIsBlockedNewUserShouldReturnFalse()
        {
            Assert.IsFalse(BruteForceProtector.IsBlocked("user1"));
        }

        [TestMethod]
        public void TestRegisterFailedAttemptUnderLimitShouldNotBlock()
        {
            BruteForceProtector.RegisterFailedAttempt("user2");
            BruteForceProtector.RegisterFailedAttempt("user2");
            Assert.IsFalse(BruteForceProtector.IsBlocked("user2"));
        }

        [TestMethod]
        public void TestRegisterFailedAttemptMaxAttemptsShouldBlockUser()
        {
            string user = "attacker";

            for (int i = 0; i < 5; i++)
            {
                BruteForceProtector.RegisterFailedAttempt(user);
            }

            Assert.IsTrue(BruteForceProtector.IsBlocked(user));
        }

        [TestMethod]
        public void TestRegisterSuccessShouldClearAttempts()
        {
            string user = "clumsy_user";
            BruteForceProtector.RegisterFailedAttempt(user);
            BruteForceProtector.RegisterFailedAttempt(user);
            BruteForceProtector.RegisterSuccess(user);

            var field = typeof(BruteForceProtector).GetField("attempts", BindingFlags.NonPublic | BindingFlags.Static);
            var attempts = (ConcurrentDictionary<string, AttemptInfo>)field.GetValue(null);
            Assert.IsFalse(attempts.ContainsKey(user));
        }
    }
}
