using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.UtilitiesTests
{
    [TestClass]
    public class LogManagerTests
    {
        [TestMethod]
        public void TestLogFatalDoesNotThrowException()
        {
            var exception = new Exception("Fatal Error");

            try
            {
                LogManager.LogFatal(exception, "TestMethod");
            }
            catch (Exception ex) 
            {
                Assert.Fail($"LogFatal should not throw {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void TestLogErrorDoesNotThrowException()
        {
            var exception = new Exception("Error");

            try
            {
                LogManager.LogError(exception, "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"LogError should not throw {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void TestLogWarnDoesNotThrowException()
        {
            string message = "Warning message";

            try
            {
                LogManager.LogWarn(message, "TestMethod");
            }
            catch
            {
                Assert.Fail("LogWarn should not throw");
            }

            Assert.AreEqual("Warning message", message);
        }

        [TestMethod]
        public void TestLogWarnHandlesNullMessage()
        {
            string message = null;

            try
            {
                LogManager.LogWarn(message, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should handle null message");
            }

            Assert.IsNull(message);
        }

        [TestMethod]
        public void TestLogErrorHandlesNullException()
        {
            Exception exception = null;

            try
            {
                LogManager.LogError(exception, "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should handle null exception {ex.Message}");
            }

            Assert.IsNull(exception);
        }
    }
}