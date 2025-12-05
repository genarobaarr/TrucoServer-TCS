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
    public class GuestJoinOptionsTests
    {
        [TestMethod]
        public void TestPlayerUsernameSetValidNameReturnsName()
        {
            var options = new GuestJoinOptions();
            string username = "GuestUser1";
            options.PlayerUsername = username;
            Assert.AreEqual(username, options.PlayerUsername);
        }

        [TestMethod]
        public void TestMatchCodeSetEmptyStringReturnsEmpty()
        {
            var options = new GuestJoinOptions();
            string emptyCode = "";
            options.MatchCode = emptyCode;
            Assert.AreEqual(string.Empty, options.MatchCode);
        }

        [TestMethod]
        public void TestLobbySetObjectReturnsObject()
        {
            var options = new GuestJoinOptions();
            var lobby = new Lobby();
            options.Lobby = lobby;
            Assert.AreSame(lobby, options.Lobby);
        }

        [TestMethod]
        public void TestPlayerUsernameSetNullReturnsNull()
        {
            var options = new GuestJoinOptions();
            options.PlayerUsername = null;
            Assert.IsNull(options.PlayerUsername);
        }

        [TestMethod]
        public void TestMatchCodeSetSpecialCharsReturnsExactString()
        {
            var options = new GuestJoinOptions();
            string special = "!@#$%^&*";
            options.MatchCode = special;
            Assert.AreEqual(special, options.MatchCode);
        }
    }
}