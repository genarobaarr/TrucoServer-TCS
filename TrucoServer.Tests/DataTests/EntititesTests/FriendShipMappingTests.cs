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
        private const int USER_ID = 100;
        private const int SECOND_USER_ID = 200;
        private const int NEGATIVE_USER_ID = -1;

        [TestMethod]
        public void TestUserIDSetValidIdReturnsId()
        {
            var mapping = new FriendShipMapping();
            int userId = USER_ID;
            mapping.UserID = userId;
            Assert.AreEqual(userId, mapping.UserID);
        }

        [TestMethod]
        public void TestFriendIDSetValidIdReturnsId()
        {
            var mapping = new FriendShipMapping();
            int userId = SECOND_USER_ID;
            mapping.FriendID = userId;
            Assert.AreEqual(userId, mapping.FriendID);
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
            int negativeId = NEGATIVE_USER_ID;
            mapping.FriendID = negativeId;
            Assert.AreEqual(negativeId, mapping.FriendID);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var mapping = new FriendShipMapping();
            Assert.IsNotNull(mapping);
        }
    }
}
