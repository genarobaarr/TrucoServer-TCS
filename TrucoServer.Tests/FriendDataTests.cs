using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class FriendDataTests
    {
        [TestMethod]
        public void FriendDataTestsTrue()
        {
            var friend = new FriendData
            {
                Username = "test",
                AvatarId = "avatar_aaa_default"
            };

            Assert.AreEqual("test", friend.Username);
            Assert.AreEqual("avatar_aaa_default", friend.AvatarId);
        }
    }
}
