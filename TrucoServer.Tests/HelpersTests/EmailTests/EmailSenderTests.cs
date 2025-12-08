using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Helpers.Email;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.HelpersTests.EmailTests
{
    [TestClass]
    public class EmailSenderTests
    {
        [TestMethod]
        public void TestSendEmailHandlesMissingConfigurationGracefully()
        {
            var sender = new EmailSender();

            try
            {
                var options = new EmailFormatOptions
                {
                    ToEmail = "test@gmail.com",
                    EmailSubject = "Subj",
                    EmailBody = "Body"
                };

                sender.SendEmail(options);
            }
            catch
            {
                Assert.Fail("Should handle exception internally");
            }

            Assert.IsNotNull(sender);
        }

        [TestMethod]
        public void TestSendLoginEmailAsyncHandlesExceptionRun()
        {
            var sender = new EmailSender();

            var user = new User
            {
                email = "test@gmail.com",
                username = "test"
            };

            sender.SendLoginEmailAsync(user);

            Assert.IsNotNull(sender);
        }

        [TestMethod]
        public void TestNotifyPasswordChangeDoesNotThrow()
        {
            var sender = new EmailSender();

            var user = new User
            {
                email = "test@gmail.com",
                username = "test"
            };

            sender.NotifyPasswordChange(user);

            Assert.IsNotNull(sender);
        }

        [TestMethod]
        public void TestSendEmailHandlesNullArguments()
        {
            var sender = new EmailSender();
            sender.SendEmail(null);

            Assert.IsNotNull(sender);
        }

        [TestMethod]
        public void TestSendLoginEmailAsyncHandlesNullUser()
        {
            var sender = new EmailSender();
            sender.SendLoginEmailAsync(null);

            Assert.IsNotNull(sender);
        }
    }
}
