using Castle.Core.Smtp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Verification;

namespace TrucoServer.Tests.HelpersTests.VerificationTests
{
    [TestClass]
    public class VerificationServiceTests
    {
        private Mock<IUserAuthenticationHelper> mockAuth;
        private Mock<Helpers.Email.IEmailSender> mockEmail;
        private VerificationService service;

        [TestInitialize]
        public void Setup()
        {
            mockAuth = new Mock<IUserAuthenticationHelper>();
            mockEmail = new Mock<Helpers.Email.IEmailSender>();
            service = new VerificationService(mockAuth.Object, mockEmail.Object);
        }

        [TestMethod]
        public void TestRequestEmailVerificationValidEmailShouldSendCodeAndReturnTrue()
        {
            string email = "valid@gmail.com";
            string code = "123456";
            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns(code);

            bool result = service.RequestEmailVerification(email, "en-US");

            Assert.IsTrue(result);
            mockEmail.Verify(e => e.SendEmail(It.Is<string>(s => s == email), It.IsAny<string>(), It.Is<string>(b => b.Contains(code))), Times.Once);
        }

        [TestMethod]
        public void TestRequestEmailVerificationInvalidEmailShouldReturnFalse()
        {
            string email = "invalid-email";

            bool result = service.RequestEmailVerification(email, "en-US");

            Assert.IsFalse(result);
            mockEmail.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestConfirmEmailVerificationCorrectCodeShouldReturnTrue()
        {
            string email = "user@gmail.com";
            string code = "999999";
            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns(code);
            service.RequestEmailVerification(email, "en-US");

            bool result = service.ConfirmEmailVerification(email, code);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestConfirmEmailVerificationIncorrectCodeShouldReturnFalse()
        {
            string email = "user@gmail.com";
            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns("111111");
            service.RequestEmailVerification(email, "en-US");

            bool result = service.ConfirmEmailVerification(email, "222222");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestConfirmEmailVerificationCodeUsedTwiceShouldReturnFalse()
        {
            string email = "willy@gmail.com";
            string code = "555555";
            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns(code);
            service.RequestEmailVerification(email, "en-US");

            service.ConfirmEmailVerification(email, code);
            bool secondAttempt = service.ConfirmEmailVerification(email, code);

            Assert.IsFalse(secondAttempt);
        }
    }
}
