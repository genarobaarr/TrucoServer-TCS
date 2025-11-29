using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Password;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.HelpersTests.PasswordTests
{
    [TestClass]
    public class PasswordManagerTests
    {
        private Mock<Helpers.Email.IEmailSender> mockEmailSender;
        private PasswordManager passwordManager;
        private const string TEST_EMAIL = "test@gmail.com";
        private const string OLD_PASS = "OldPass123";

        [TestInitialize]
        public void Setup()
        {
            mockEmailSender = new Mock<Helpers.Email.IEmailSender>();
            passwordManager = new PasswordManager(mockEmailSender.Object);

            using (var context = new baseDatosTrucoEntities())
            {
                var existing = context.User.FirstOrDefault(u => u.email == TEST_EMAIL);
               
                if (existing != null)
                {
                    context.User.Remove(existing);
                    context.SaveChanges();
                }

                var user = new User
                {
                    username = "TestUser",
                    email = TEST_EMAIL,
                    passwordHash = PasswordHasher.Hash(OLD_PASS),
                    nameChangeCount = 0,
                };
                
                context.User.Add(user);
                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.email == TEST_EMAIL);
                
                if (user != null)
                {
                    context.User.Remove(user);
                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestUpdatePasswordAndNotifyValidUserShouldUpdateHashAndSendEmail()
        {
            string newPass = "NewPass456";
            string lang = "en-US";
            string method = "TestUpdate";

            bool result = passwordManager.UpdatePasswordAndNotify(TEST_EMAIL, newPass, lang, method);

            Assert.IsTrue(result);

            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.First(u => u.email == TEST_EMAIL);
                Assert.IsTrue(PasswordHasher.Verify(newPass, user.passwordHash));
                Assert.IsFalse(PasswordHasher.Verify(OLD_PASS, user.passwordHash));
            }

            mockEmailSender.Verify(e => e.NotifyPasswordChange(It.Is<User>(u => u.email == TEST_EMAIL)), Times.Once);
        }

        [TestMethod]
        public void TestUpdatePasswordAndNotifyNonExistentUserShouldReturnFalse()
        {
            bool result = passwordManager.UpdatePasswordAndNotify("test@gmail.com", "pass", "en-US", "Test");
            Assert.IsFalse(result);
            mockEmailSender.Verify(e => e.NotifyPasswordChange(It.IsAny<User>()), Times.Never);
        }

        [TestMethod]
        public void TestUpdatePasswordAndNotifySmtpFailureShouldReturnTrueButLog()
        {
            mockEmailSender.Setup(e => e.NotifyPasswordChange(It.IsAny<User>()))
                           .Throws(new SmtpException("Server down"));

            bool result = passwordManager.UpdatePasswordAndNotify(TEST_EMAIL, "NewPass", "en-US", "Test");
            Assert.IsTrue(result, "Should return true because DB update succeeded despite email failure");

            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.First(u => u.email == TEST_EMAIL);
                Assert.IsTrue(PasswordHasher.Verify("NewPass", user.passwordHash));
            }
        }
    }
}
