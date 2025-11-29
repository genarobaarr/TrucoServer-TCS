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
        public void TestLogInfoSmokeTestShouldNotThrowException()
        {
            try
            {
                LogManager.LogWarn("Test Message", "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"LogWarn threw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestLogErrorSmokeTestShouldNotThrowException()
        {
            try
            {
                LogManager.LogError(new Exception("Test Ex"), "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"LogError threw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestLogFatalSmokeTestShouldNotThrowException()
        {
            try
            {
                LogManager.LogFatal(new Exception("Fatal Ex"), "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"LogFatal threw exception: {ex.Message}");
            }
        }
    }
}
