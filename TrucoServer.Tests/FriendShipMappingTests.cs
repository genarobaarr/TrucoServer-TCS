using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class FriendShipMappingTests
    {
        private const int TEST_EXPECTED_CORRECT_VALUE = 5;
        private const int TEST_EXPECTED_SECOND_CORRECT_VALUE = 10;
        private const string TEST_USER_ID_PROPERTY_NAME = "userID";
        private const string TEST_FRIEND_ID_PROPERTY_NAME = "friendID";
        private const string TEST_USER_ID_COLUMN_NAME = "userID1";
        private const string TEST_FRIEND_ID_COLUMN_NAME = "userID2";

        [TestMethod]
        public void TestUserIDSetReturnsCorrectValue()
        {
            var mapping = new FriendShipMapping();
            int expected = TEST_EXPECTED_CORRECT_VALUE;

            mapping.userID = expected;

            Assert.AreEqual(expected, mapping.userID);
        }

        [TestMethod]
        public void TestFriendIDSetReturnsCorrectValue()
        {
            var mapping = new FriendShipMapping();
            int expected = TEST_EXPECTED_SECOND_CORRECT_VALUE;

            mapping.friendID = expected;

            Assert.AreEqual(expected, mapping.friendID);
        }

        [TestMethod]
        public void TestFriendshipClassHasMetadataTypeAttribute()
        {
            var type = typeof(Friendship);

            var attribute = type.GetCustomAttribute<MetadataTypeAttribute>();

            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void TestFriendshipMetadataTypeIsFriendShipMapping()
        {
            var type = typeof(Friendship);
            var attribute = type.GetCustomAttribute<MetadataTypeAttribute>();

            Assert.AreEqual(typeof(FriendShipMapping), attribute.MetadataClassType);
        }

        [TestMethod]
        public void TestUserIDPropertyHasColumnAttribute()
        {
            var property = typeof(FriendShipMapping).GetProperty(TEST_USER_ID_PROPERTY_NAME);

            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void TestUserIDColumnAttributeHasCorrectName()
        {
            var property = typeof(FriendShipMapping).GetProperty(TEST_USER_ID_PROPERTY_NAME);
            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.AreEqual(TEST_USER_ID_COLUMN_NAME, attribute.Name);
        }

        [TestMethod]
        public void TestFriendIDPropertyHasColumnAttribute()
        {
            var property = typeof(FriendShipMapping).GetProperty(TEST_FRIEND_ID_PROPERTY_NAME);

            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void TestFriendIDColumnAttributeHasCorrectName()
        {
            var property = typeof(FriendShipMapping).GetProperty(TEST_FRIEND_ID_PROPERTY_NAME);
            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.AreEqual(TEST_FRIEND_ID_COLUMN_NAME, attribute.Name);
        }
    }
}