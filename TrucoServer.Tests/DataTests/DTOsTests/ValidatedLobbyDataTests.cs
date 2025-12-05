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
    public class ValidatedLobbyDataTests
    {
        [TestMethod]
        public void TestLobbySetObjectReturnsObject()
        {
            var data = new ValidatedLobbyData();
            var lobby = new Lobby();
            data.Lobby = lobby;
            Assert.AreSame(lobby, data.Lobby);
        }

        [TestMethod]
        public void TestMembersSetListReturnsList()
        {
            var data = new ValidatedLobbyData();
            var members = new List<LobbyMember>();
            data.Members = members;
            Assert.AreSame(members, data.Members);
        }
    }
}
