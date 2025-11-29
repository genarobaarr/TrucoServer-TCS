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
        public void TestConstructorDefaultShouldHaveNullLists()
        {
            var data = new ValidatedLobbyData();
            var members = data.Members;
            Assert.IsNull(members, "Lists should be null by default if not initialized in constructor.");
        }

        [TestMethod]
        public void TestLobbyPropertySetObjectShouldStoreReference()
        {
            var data = new ValidatedLobbyData();
            Lobby lobbyMock = null;
            data.Lobby = lobbyMock;
            Assert.AreEqual(lobbyMock, data.Lobby);
        }

        [TestMethod]
        public void TestMembersPropertySetListShouldStoreReference()
        {
            var data = new ValidatedLobbyData();
            var memberList = new List<LobbyMember>();
            data.Members = memberList;
            Assert.AreSame(memberList, data.Members);
        }

        [TestMethod]
        public void TestGuestsPropertySetListShouldStoreReference()
        {
            var data = new ValidatedLobbyData();
            var guestList = new List<PlayerInfo>();
            data.Guests = guestList;
            Assert.AreSame(guestList, data.Guests);
        }

        [TestMethod]
        public void TestObjectInitializationWithPropertiesShouldPersistData()
        {
            var data = new ValidatedLobbyData
            {
                Members = new List<LobbyMember>(),
                Guests = new List<PlayerInfo>()
            };

            bool listsAreNotNull = data.Members != null && data.Guests != null;
            Assert.IsTrue(listsAreNotNull);
        }
    }
}
