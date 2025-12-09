using Castle.Core.Smtp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Verification;
using IEmailSender = TrucoServer.Helpers.Email.IEmailSender;

namespace TrucoServer.Tests.HelpersTests.VerificationTests
{
    [TestClass]
    public class VerificationServiceTests
    {
        private Mock<IUserAuthenticationHelper> mockAuth;
        private Mock<IEmailSender> mockEmail;
        private VerificationService service;

        [TestInitialize]
        public void Setup()
        {
            mockAuth = new Mock<IUserAuthenticationHelper>();
            mockEmail = new Mock<IEmailSender>();
            service = new VerificationService(mockAuth.Object, mockEmail.Object);
        }

        [TestMethod]
        public void TestRequestEmailVerificationReturnsFalseForInvalidEmailFormat()
        {
            var service = new VerificationService(mockAuth.Object, mockEmail.Object);
            string invalidEmail = "esto_no_es_un_email";
            bool result = service.RequestEmailVerification(invalidEmail, "es-MX");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRequestEmailVerificationThrowsFaultExceptionOnEmailSenderFailure()
        {
            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns("123456");
            mockEmail.Setup(e => e.SendEmail(It.IsAny<EmailFormatOptions>()))
                     .Throws(new Exception("SMTP Error"));

            Assert.ThrowsException<FaultException<CustomFault>>(() =>
                service.RequestEmailVerification("valid@gmail.com", "es-MX"));
        }

        [TestMethod]
        public void TestConfirmEmailVerificationReturnsFalseForNullEmail()
        {
            var service = new VerificationService(mockAuth.Object, mockEmail.Object);
            bool result = service.ConfirmEmailVerification(null, "123456");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestConfirmEmailVerificationReturnsFalseForCodeMismatch()
        {
            var service = new VerificationService(mockAuth.Object, mockEmail.Object);
            string email = "test@gmail.com";
            string correctCode = "123456";

            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns(correctCode);
            service.RequestEmailVerification(email, "es-MX");
            bool result = service.ConfirmEmailVerification(email, "999999");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestConfirmEmailVerificationReturnsFalseOnReplayAttack()
        {
            var service = new VerificationService(mockAuth.Object, mockEmail.Object);
            string email = "test@gmail.com";
            string code = "123456";
            mockAuth.Setup(a => a.GenerateSecureNumericCode()).Returns(code);
            service.RequestEmailVerification(email, "es-MX");
            service.ConfirmEmailVerification(email, code);
            bool result = service.ConfirmEmailVerification(email, code);
            Assert.IsFalse(result);
        }
    }
}
