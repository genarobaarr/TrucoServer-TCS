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
    public class LobbyLeaveCriteriaTests
    {
        [TestMethod]
        public void TestMatchCodeSetValidCodeReturnsCode()
        {
            var criteria = new LobbyLeaveCriteria();
            string code = "ABC123";
            criteria.MatchCode = code;
            Assert.AreEqual(code, criteria.MatchCode);
        }

        [TestMethod]
        public void TestUsernameSetValidNameReturnsName()
        {
            var criteria = new LobbyLeaveCriteria();
            string user = "User123";
            criteria.Username = user;
            Assert.AreEqual(user, criteria.Username);
        }

        [TestMethod]
        public void TestMatchCodeSetNullReturnsNull()
        {
            var criteria = new LobbyLeaveCriteria();
            criteria.MatchCode = null;
            Assert.IsNull(criteria.MatchCode);
        }

        [TestMethod]
        public void TestUsernameSetEmptyReturnsEmpty()
        {
            var criteria = new LobbyLeaveCriteria();
            criteria.Username = string.Empty;
            Assert.AreEqual(string.Empty, criteria.Username);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var criteria = new LobbyLeaveCriteria();
            Assert.IsNotNull(criteria);
        }
    }
}
