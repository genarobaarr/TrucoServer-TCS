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
    public class LobbyLeaveResultTests
    {
        [TestMethod]
        public void TestLobbySetNullReturnsNull()
        {
            var result = new LobbyLeaveResult();
            result.Lobby = null;
            Assert.IsNull(result.Lobby);
        }

        [TestMethod]
        public void TestPlayerSetNullReturnsNull()
        {
            var result = new LobbyLeaveResult();
            result.Player = null;
            Assert.IsNull(result.Player);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var result = new LobbyLeaveResult();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestLobbySetObjectReturnsObject()
        {
            var result = new LobbyLeaveResult();
            var lobby = new Lobby();
            result.Lobby = lobby;
            Assert.AreSame(lobby, result.Lobby);
        }

        [TestMethod]
        public void TestPlayerSetObjectReturnsObject()
        {
            var result = new LobbyLeaveResult();
            var player = new User();
            result.Player = player;
            Assert.AreSame(player, result.Player);
        }
    }
}
