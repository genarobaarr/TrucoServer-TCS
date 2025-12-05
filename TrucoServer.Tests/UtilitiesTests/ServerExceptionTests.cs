using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.UtilitiesTests
{
    [TestClass]
    public class ServerExceptionTests
    {
        [TestMethod]
        public void TestHandleExceptionWithArgumentExceptionDoesNotThrow()
        {
            var ex = new ArgumentException("Invalid argument");

            try
            {
                ServerException.HandleException(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should not throw exception");
            }

            Assert.IsTrue(true); 
        }

        [TestMethod]
        public void TestHandleExceptionWithFileNotFoundExceptionDoesNotThrow()
        {
            var ex = new FileNotFoundException("Missing file");

            try
            {
                ServerException.HandleException(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should not throw exception");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestHandleExceptionWithGenericExceptionDoesNotThrow()
        {
            var ex = new Exception("Generic error");

            try
            {
                ServerException.HandleException(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should not throw exception");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestHandleExceptionWithInvalidOperationExceptionDoesNotThrow()
        {
            var ex = new InvalidOperationException("Bad op");

            try
            {
                ServerException.HandleException(ex, "TestMethod");
            }
            catch
            {
                Assert.Fail("Should not throw exception");
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestHandleExceptionWithNullMethodNameDoesNotThrow()
        {
            var ex = new Exception("Error");

            try
            {
                ServerException.HandleException(ex, null);
            }
            catch
            {
                Assert.Fail("Should not throw exception");
            }

            Assert.IsTrue(true);
        }
    }
}
