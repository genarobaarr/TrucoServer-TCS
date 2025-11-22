using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests
{
    [TestClass]
    public class FriendDataTests
    {
        private const string TEST_USERNAME = "test";
        private const string TEST_AVATAR_ID = "avatar_aaa_default";

        private FriendData GetSampleFriendData()
        {
            return new FriendData
            {
                Username = TEST_USERNAME,
                AvatarId = TEST_AVATAR_ID
            };
        }

        [TestMethod]
        public void TestFriendDataUsernameBeSame()
        {
            var friend = GetSampleFriendData();
            Assert.AreEqual(TEST_USERNAME, friend.Username);
        }

        [TestMethod]
        public void TestFriendDataAvatarIdBeAvatarAaaDefaultTrue()
        {
            var friend = GetSampleFriendData();
            Assert.AreEqual(TEST_AVATAR_ID, friend.AvatarId);
        }
    }
}
