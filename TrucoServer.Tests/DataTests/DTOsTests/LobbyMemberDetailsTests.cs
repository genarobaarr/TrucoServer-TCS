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
    public class LobbyMemberDetailsTests
    {
        [TestMethod]
        public void TestLobbyIdSetValidIdReturnsId()
        {
            var member = new LobbyMemberDetails();
            int id = 50;
            member.LobbyId = id;
            Assert.AreEqual(id, member.LobbyId);
        }

        [TestMethod]
        public void TestUserIdSetValidIdReturnsId()
        {
            var member = new LobbyMemberDetails();
            int userId = 99;
            member.UserId = userId;
            Assert.AreEqual(userId, member.UserId);
        }

        [TestMethod]
        public void TestRoleSetStringReturnsString()
        {
            var member = new LobbyMemberDetails();
            string role = "Admin";
            member.Role = role;
            Assert.AreEqual(role, member.Role);
        }

        [TestMethod]
        public void TestTeamSetStringReturnsString()
        {
            var member = new LobbyMemberDetails();
            string team = "Team 1";
            member.Team = team;
            Assert.AreEqual(team, member.Team);
        }

        [TestMethod]
        public void TestRoleSetNullReturnsNull()
        {
            var member = new LobbyMemberDetails();
            member.Role = null;
            Assert.IsNull(member.Role);
        }
    }
}
