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
        public void TestUserIDSetValidIdReturnsId()
        {
            var mapping = new FriendShipMapping();
            int id = 100;
            mapping.UserID = id;
            Assert.AreEqual(id, mapping.UserID);
        }

        [TestMethod]
        public void TestFriendIDSetValidIdReturnsId()
        {
            var mapping = new FriendShipMapping();
            int id = 200;
            mapping.FriendID = id;
            Assert.AreEqual(id, mapping.FriendID);
        }

        [TestMethod]
        public void TestUserIDSetMaxValueReturnsMaxValue()
        {
            var mapping = new FriendShipMapping();
            int max = int.MaxValue;
            mapping.UserID = max;
            Assert.AreEqual(max, mapping.UserID);
        }

        [TestMethod]
        public void TestFriendIDSetNegativeReturnsNegative()
        {
            var mapping = new FriendShipMapping();
            int negative = -1;
            mapping.FriendID = negative;
            Assert.AreEqual(negative, mapping.FriendID);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var mapping = new FriendShipMapping();
            Assert.IsNotNull(mapping);
        }
    }
}
