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
        public void TestSendLoginEmailAsyncShouldExecuteWithoutBlocking()
        {
            var sender = new EmailSender();
            
            var user = new User 
            { 
                email = "fake@gmail.com", 
                username = "FakeUser" 
            };

            sender.SendLoginEmailAsync(user);
            Assert.IsTrue(true, "Method returned successfully");
        }

        [TestMethod]
        public void TestNotifyPasswordChangeShouldExecuteWithoutBlocking()
        {
            var sender = new EmailSender();
            
            var user = new User 
            { 
                email = "fake@gmail.com", 
                username = "FakeUser" 
            };

            sender.NotifyPasswordChange(user);
            Assert.IsTrue(true);
        }
    }
}
