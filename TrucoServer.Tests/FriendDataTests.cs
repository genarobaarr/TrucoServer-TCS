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
        private FriendData GetSampleFriendData()
        {
            return new FriendData
            {
                Username = "test",
                AvatarId = "avatar_aaa_default"
            };
        }

        [TestMethod]
        public void TestFriendDataUsernameBeSame()
        {
            var friend = GetSampleFriendData();
            Assert.AreEqual("test", friend.Username);
        }

        [TestMethod]
        public void TestFriendDataAvatarIdBeAvatarAaaDefaultTrue()
        {
            var friend = GetSampleFriendData();
            Assert.AreEqual("avatar_aaa_default", friend.AvatarId);
        }
    }
}
