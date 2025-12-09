using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TrucoServer.Helpers.Friends;

namespace TrucoServer.Tests.HelpersTests.FriendsTests
{
    [TestClass]
    public class FriendNotifierTests
    {
        [TestMethod]
        public void TestNotifyRequestReceivedHandlesExceptionGracefully()
        {
            var notifier = new FriendNotifier();

            try
            {
                notifier.NotifyRequestReceived("Target", "Sender");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should handle exception internally {ex.Message}");
            }

            Assert.IsNotNull(notifier);
        }

        [TestMethod]
        public void TestNotifyRequestAcceptedHandlesExceptionGracefully()
        {
            var notifier = new FriendNotifier();

            try
            {
                notifier.NotifyRequestAccepted("Target", "Sender");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should handle exception internally {ex.Message}");
            }

            Assert.IsNotNull(notifier);
        }

        [TestMethod]
        public void TestNotifyRequestReceivedDoesNotCrashWithNullInputs()
        {
            var notifier = new FriendNotifier();
            notifier.NotifyRequestReceived(null, null);
            Assert.IsNotNull(notifier);
        }

        [TestMethod]
        public void TestNotifyRequestAcceptedDoesNotCrashWithNullInputs()
        {
            var notifier = new FriendNotifier();
            notifier.NotifyRequestAccepted(null, null);
            Assert.IsNotNull(notifier);
        }
    }
}