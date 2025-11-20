using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity.Hierarchy;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    /*
    [TestClass]
    public class LogManagerTests
    {
        private MemoryAppender memoryAppender;

        [TestInitialize]
        public void Setup()
        {
            memoryAppender = new MemoryAppender();
            var repository = (Hierarchy)log4net.LogManager.GetRepository();
            repository.Root.AddAppender(memoryAppender);
            repository.Configured = true;
            BasicConfigurator.Configure(repository, memoryAppender);
        }

        [TestCleanup]
        public void Cleanup()
        {
            memoryAppender.Clear();
        }

        [TestMethod]
        public void TestLogFatalLogsCorrectMessage()
        {
            var exception = new Exception("Fatal Error");
            string methodName = "TestMethod";

            LogManager.LogFatal(exception, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.IsTrue(loggedEvent.RenderedMessage.Contains(methodName));
        }

        [TestMethod]
        public void TestLogFatalLogsCorrectLevel()
        {
            var exception = new Exception("Fatal Error");
            string methodName = "TestMethod";

            LogManager.LogFatal(exception, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.AreEqual(Level.Fatal, loggedEvent.Level);
        }

        [TestMethod]
        public void TestLogFatalLogsExceptionObject()
        {
            var exception = new Exception("Fatal Error");
            string methodName = "TestMethod";

            LogManager.LogFatal(exception, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.AreEqual(exception.Message, loggedEvent.ExceptionObject.Message);
        }

        [TestMethod]
        public void TestLogErrorLogsCorrectMessage()
        {
            var exception = new InvalidOperationException("Op Error");
            string methodName = "BusinessMethod";

            LogManager.LogError(exception, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.IsTrue(loggedEvent.RenderedMessage.Contains("Error operativo"));
        }

        [TestMethod]
        public void TestLogErrorLogsCorrectLevel()
        {
            var exception = new InvalidOperationException("Op Error");
            string methodName = "BusinessMethod";

            LogManager.LogError(exception, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.AreEqual(Level.Error, loggedEvent.Level);
        }

        [TestMethod]
        public void TestLogWarnLogsCorrectMessage()
        {
            string msg = "Low disk space";
            string methodName = "CheckDisk";

            LogManager.LogWarn(msg, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.IsTrue(loggedEvent.RenderedMessage.Contains(msg));
        }

        [TestMethod]
        public void TestLogWarnLogsCorrectLevel()
        {
            string msg = "Warning content";
            string methodName = "CheckDisk";

            LogManager.LogWarn(msg, methodName);
            var loggedEvent = memoryAppender.GetEvents().FirstOrDefault();

            Assert.AreEqual(Level.Warn, loggedEvent.Level);
        }
    }
    */
}