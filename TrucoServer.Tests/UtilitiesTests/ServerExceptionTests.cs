using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.UtilitiesTests
{
    [TestClass]
    public class ServerExceptionTests
    {
        [TestMethod]
        public void TestHandleExceptionWithArgumentExceptionDoesNotThrow()
        {
            var exception = new ArgumentException("Invalid argument");

            try
            {
                ServerException.HandleException(exception, "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw exception {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void TestHandleExceptionWithFileNotFoundExceptionDoesNotThrow()
        {
            var exception = new FileNotFoundException("Missing file");

            try
            {
                ServerException.HandleException(exception, "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw exception {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void TestHandleExceptionWithGenericExceptionDoesNotThrow()
        {
            var exception = new Exception("Generic error");

            try
            {
                ServerException.HandleException(exception, "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw exception {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void TestHandleExceptionWithInvalidOperationExceptionDoesNotThrow()
        {
            var exception  = new InvalidOperationException("Bad op");

            try
            {
                ServerException.HandleException(exception, "TestMethod");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw exception {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void TestHandleExceptionWithNullMethodNameDoesNotThrow()
        {
            var exception = new Exception("Error");

            try
            {
                ServerException.HandleException(exception, null);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw exception {ex.Message}");
            }

            Assert.IsNotNull(exception);
        }
    }
}