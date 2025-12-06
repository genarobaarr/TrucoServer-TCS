using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Password;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.HelpersTests.PasswordTests
{
    [TestClass]
    public class PasswordManagerTests
    {
        private Mock<IEmailSender> mockEmail;

        [TestInitialize]
        public void Setup()
        {
            mockEmail = new Mock<IEmailSender>();
        }

        [TestMethod]
        public void TestUpdatePasswordAndNotifyReturnsFalseOnDbConnectionFailure()
        {
            var manager = new PasswordManager(mockEmail.Object);
            var options = new PasswordUpdateOptions
            { 
                Email = "test@gmail.com"
            };

            bool result = manager.UpdatePasswordAndNotify(options);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUpdatePasswordAndNotifyHandlesNullContext()
        {
            var manager = new PasswordManager(mockEmail.Object);
            bool result = manager.UpdatePasswordAndNotify(null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUpdatePasswordAndNotifyDoesNotCallEmailIfDbFails()
        {
            var manager = new PasswordManager(mockEmail.Object);
            var options = new PasswordUpdateOptions 
            {
                Email = "test@gmail.com"
            };

            manager.UpdatePasswordAndNotify(options);
            mockEmail.Verify(e => e.NotifyPasswordChange(It.IsAny<User>()), Times.Never);
        }
    }
}
