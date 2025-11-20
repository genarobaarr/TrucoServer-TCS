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
        [TestMethod]
        public void TestUserIDSetReturnsCorrectValue()
        {
            var mapping = new FriendShipMapping();
            int expected = 5;

            mapping.userID = expected;

            Assert.AreEqual(expected, mapping.userID);
        }

        [TestMethod]
        public void TestFriendIDSetReturnsCorrectValue()
        {
            var mapping = new FriendShipMapping();
            int expected = 10;

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
            var property = typeof(FriendShipMapping).GetProperty("userID");

            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void TestUserIDColumnAttributeHasCorrectName()
        {
            var property = typeof(FriendShipMapping).GetProperty("userID");
            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.AreEqual("userID1", attribute.Name);
        }

        [TestMethod]
        public void TestFriendIDPropertyHasColumnAttribute()
        {
            var property = typeof(FriendShipMapping).GetProperty("friendID");

            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void TestFriendIDColumnAttributeHasCorrectName()
        {
            var property = typeof(FriendShipMapping).GetProperty("friendID");
            var attribute = property.GetCustomAttribute<ColumnAttribute>();

            Assert.AreEqual("userID2", attribute.Name);
        }
    }
}