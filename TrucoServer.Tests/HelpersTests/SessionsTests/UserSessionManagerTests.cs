using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests.HelpersTests.SessionsTests
{
    [TestClass]
    public class UserSessionManagerTests
    {
        private Helpers.Sessions.UserSessionManager sessionManager;

        [TestInitialize]
        public void Setup()
        {
            sessionManager = new Helpers.Sessions.UserSessionManager();
        }

        [TestMethod]
        public void TestGetUserCallbackNonRegisteredUserShouldReturnNull()
        {
            var callback = sessionManager.GetUserCallback("TestUser");
            Assert.IsNull(callback);
        }

        [TestMethod]
        public void TestHandleExistingSessionNewUserShouldNotThrow()
        {
            try
            {
                sessionManager.HandleExistingSession("NewUser");
            }
            catch (Exception ex) 
            {
                Assert.Fail($"Should not throw exception for new user {ex.Message}");
            }
        }
    }
}
