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
    public class PlayerInformationTests
    {
        [TestMethod]
        public void TestConstructorSetsPlayerIDCorrectly()
        {
            int id = 100;
            string user = "User1";
            string team = "Team 1";
            var player = new PlayerInformation(id, user, team);
            Assert.AreEqual(id, player.PlayerID);
        }

        [TestMethod]
        public void TestConstructorSetsUsernameCorrectly()
        {
            string user = "User1";
            var player = new PlayerInformation(1, user, "Team 1");
            Assert.AreEqual(user, player.Username);
        }

        [TestMethod]
        public void TestConstructorSetsTeamCorrectly()
        {
            string team = "Team 2";
            var player = new PlayerInformation(1, "User1", team);
            Assert.AreEqual(team, player.Team);
        }
    }
}
