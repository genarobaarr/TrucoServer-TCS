using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class FriendRequestTests
    {
        [TestMethod]
        public void TestRequesterIdSetValidIdReturnsId()
        {
            var request = new FriendRequest();
            int id = 100;
            request.RequesterId = id;
            Assert.AreEqual(id, request.RequesterId);
        }

        [TestMethod]
        public void TestTargetIdSetValidIdReturnsId()
        {
            var request = new FriendRequest();
            int id = 200;
            request.TargetId = id;
            Assert.AreEqual(id, request.TargetId);
        }

        [TestMethod]
        public void TestStatusSetPendingReturnsPending()
        {
            var request = new FriendRequest();
            string status = "Pending";
            request.Status = status;
            Assert.AreEqual(status, request.Status);
        }

        [TestMethod]
        public void TestStatusSetNullReturnsNull()
        {
            var request = new FriendRequest();
            request.Status = null;
            Assert.IsNull(request.Status);
        }

        [TestMethod]
        public void TestRequesterIdSetZeroReturnsZero()
        {
            var request = new FriendRequest();
            request.RequesterId = 0;
            Assert.AreEqual(0, request.RequesterId);
        }
    }
}
