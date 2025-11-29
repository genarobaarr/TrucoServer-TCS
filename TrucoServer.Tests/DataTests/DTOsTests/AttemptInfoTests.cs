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
    public class AttemptInfoTests
    {
        [TestMethod]
        public void TestConstructorShouldInitializeWithDefaultValues()
        {
            var attempt = new AttemptInfo();
            int failedCount = attempt.FailedCount;
            Assert.AreEqual(0, failedCount, "The fault counter should start at 0");
        }

        [TestMethod]
        public void TestDefaultDateShouldBeMinValue()
        {
            var attempt = new AttemptInfo();
            DateTime blockedDate = attempt.BlockedUntil;
            Assert.AreEqual(DateTime.MinValue, blockedDate, "The default lock date must be MinValue");
        }

        [TestMethod]
        public void TestSetPropertiesShouldStoreCorrectValues()
        {
            var attempt = new AttemptInfo();
            int expectedCount = 5;
            attempt.FailedCount = expectedCount;
            Assert.AreEqual(expectedCount, attempt.FailedCount);
        }

        [TestMethod]
        public void TestNegativeCountShouldStoreValue()
        {
            var attempt = new AttemptInfo();
            attempt.FailedCount = -1;
            Assert.AreEqual(-1, attempt.FailedCount);
        }

        [TestMethod]
        public void TestFutureDateShouldBeStored()
        {
            var attempt = new AttemptInfo();
            DateTime futureDate = DateTime.Now.AddMinutes(15);
            attempt.BlockedUntil = futureDate;
            Assert.AreEqual(futureDate, attempt.BlockedUntil);
        }
    }
}