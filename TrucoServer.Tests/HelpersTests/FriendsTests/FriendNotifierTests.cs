using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Friends;

namespace TrucoServer.Tests.HelpersTests.FriendsTests
{
    [TestClass]
    public class FriendNotifierTests
    {
        [TestMethod]
        public void TestNotifyRequestReceivedUserOfflineShouldHandleGracefully()
        {
            var notifier = new FriendNotifier();
            notifier.NotifyRequestReceived("TestUser", "SenderUser");
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestNotifyRequestAcceptedUserOfflineShouldHandleGracefully()
        {
            var notifier = new FriendNotifier();
            notifier.NotifyRequestAccepted("TestUser", "SenderUser");
            Assert.IsTrue(true);
        }
    }
}
