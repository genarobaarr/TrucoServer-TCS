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
    public class FriendshipCommitOptionsTests
    {
        [TestMethod]
        public void TestConstructorInstanceNotNull()
        {
            var options = new FriendshipCommitOptions();
            Assert.IsNotNull(options);
        }

        [TestMethod]
        public void TestRequesterIdSetMaxValueReturnsExpectedValue()
        {
            var options = new FriendshipCommitOptions();
            int expectedId = int.MaxValue;
            options.RequesterId = expectedId;
            Assert.AreEqual(expectedId, options.RequesterId);
        }

        [TestMethod]
        public void TestStatusAcceptedSetNullReturnsNull()
        {
            var options = new FriendshipCommitOptions();
            options.StatusAccepted = null;
            Assert.IsNull(options.StatusAccepted);
        }

        [TestMethod]
        public void TestAcceptorIdSetNegativeValueReturnsNegativeValue()
        {
            var options = new FriendshipCommitOptions();
            int negativeId = -50;
            options.AcceptorId = negativeId;
            Assert.AreEqual(negativeId, options.AcceptorId);
        }

        [TestMethod]
        public void TestRequestSetObjectReturnsCorrectInstance()
        {
            var options = new FriendshipCommitOptions();
            var friendship = new Friendship();
            options.Request = friendship;
            Assert.AreSame(friendship, options.Request);
        }
    }
}