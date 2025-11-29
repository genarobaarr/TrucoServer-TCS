using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.Entities;

namespace TrucoServer.Tests.DataTests.EntititesTests
{
    [TestClass]
    public class FriendShipMappingTests
    {
        [TestMethod]
        public void TestFriendshipClassShouldHaveMetadataTypeAttribute()
        {
            var type = typeof(Friendship);
            var attribute = type.GetCustomAttribute<MetadataTypeAttribute>();
            Assert.IsNotNull(attribute, "The Friendship partial class must have the MetadataType attribute.");
        }

        [TestMethod]
        public void TestUserIDPropertyShouldHaveColumnAttributeUserID1()
        {
            var property = typeof(FriendShipMapping).GetProperty("UserID");
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            Assert.IsNotNull(columnAttr);
            Assert.AreEqual("userID1", columnAttr.Name);
        }

        [TestMethod]
        public void TestFriendIDPropertyShouldHaveColumnAttributeUserID2()
        {
            var property = typeof(FriendShipMapping).GetProperty("FriendID");
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            Assert.IsNotNull(columnAttr);
            Assert.AreEqual("userID2", columnAttr.Name);
        }

        [TestMethod]
        public void TestUserIDSetPropertyShouldStoreIntegerValue()
        {
            var mapping = new FriendShipMapping();
            int expectedId = 100;
            mapping.UserID = expectedId;
            Assert.AreEqual(expectedId, mapping.UserID);
        }

        [TestMethod]
        public void TestFriendIDSetNegativeValueShouldStoreValue()
        {
            var mapping = new FriendShipMapping();
            int negativeId = -50;
            mapping.FriendID = negativeId;
            Assert.AreEqual(negativeId, mapping.FriendID);
        }
    }
}
