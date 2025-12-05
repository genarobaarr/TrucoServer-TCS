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
        public void TestFailedCountDefaultValueIsZero()
        {
            var info = new AttemptInfo();
            Assert.AreEqual(0, info.FailedCount);
        }

        [TestMethod]
        public void TestBlockedUntilDefaultValueIsMinValue()
        {
            var info = new AttemptInfo();
            Assert.AreEqual(DateTime.MinValue, info.BlockedUntil);
        }

        [TestMethod]
        public void TestFailedCountIncrementReturnsIncrementedValue()
        {
            var info = new AttemptInfo();
            info.FailedCount++;
            Assert.AreEqual(1, info.FailedCount);
        }

        [TestMethod]
        public void TestBlockedUntilSetFutureDateReturnsDate()
        { 
            var info = new AttemptInfo();
            var future = DateTime.Now.AddHours(1);
            info.BlockedUntil = future;
            Assert.AreEqual(future, info.BlockedUntil);
        }

        [TestMethod]
        public void TestFailedCountSetNegativeReturnsNegative()
        {
            var info = new AttemptInfo();
            int negative = -5;
            info.FailedCount = negative;
            Assert.AreEqual(negative, info.FailedCount);
        }
    }
}