using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Email;

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
                sender.SendEmail("test@gmail.com", "Subj", "Body");
            }
            catch
            {
                Assert.Fail("Should handle exception internally");
            }

            Assert.IsTrue(true);
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
            Assert.IsTrue(true);
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
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestSendEmailHandlesNullArguments()
        {
            var sender = new EmailSender();
            sender.SendEmail(null, null, null);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestSendLoginEmailAsyncHandlesNullUser()
        {
            var sender = new EmailSender();
            sender.SendLoginEmailAsync(null);
            Assert.IsTrue(true);
        }
    }
}
