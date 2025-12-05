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
    public class LogManagerTests
    {
        [TestMethod]
        public void TestLogFatalDoesNotThrowException()
        {
            var ex = new Exception("Fatal Error");

            try
            {
                LogManager.LogFatal(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("LogFatal should not throw");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestLogErrorDoesNotThrowException()
        {
            var ex = new Exception("Error");

            try
            {
                LogManager.LogError(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("LogError should not throw");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestLogWarnDoesNotThrowException()
        {
            string msg = "Warning message";

            try
            {
                LogManager.LogWarn(msg, "TestMethod");
            }
            catch
            {
                Assert.Fail("LogWarn should not throw");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestLogWarnHandlesNullMessage()
        {
            string msg = null;

            try
            {
                LogManager.LogWarn(msg, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should handle null message");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestLogErrorHandlesNullException()
        {
            Exception ex = null;

            try
            {
                LogManager.LogError(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should handle null exception");
            }

            Assert.IsTrue(true);
        }
    }
}
