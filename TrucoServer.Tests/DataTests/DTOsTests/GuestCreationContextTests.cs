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
    public class GuestCreationContextTests
    {
        [TestMethod]
        public void TestMatchCodeSetNullReturnsNull()
        {
            var context = new GuestCreationContext();
            context.MatchCode = null;
            Assert.IsNull(context.MatchCode);
        }

        [TestMethod]
        public void TestPlayerUsernameSetLongStringReturnsString()
        {
            var context = new GuestCreationContext();
            string longName = new string('a', 100);
            context.PlayerUsername = longName;
            Assert.AreEqual(100, context.PlayerUsername.Length);
        }

        [TestMethod]
        public void TestLobbySetNullReturnsNull()
        {
            var context = new GuestCreationContext();
            context.Lobby = null;
            Assert.IsNull(context.Lobby);
        }

        [TestMethod]
        public void TestMatchCodeSetValidCodeReturnsCode()
        {
            var context = new GuestCreationContext();
            string code = "MATCH55";
            context.MatchCode = code;
            Assert.AreEqual(code, context.MatchCode);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var context = new GuestCreationContext();
            Assert.IsNotNull(context);
        }
    }
}
