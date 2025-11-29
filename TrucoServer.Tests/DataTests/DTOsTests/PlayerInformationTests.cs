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
        public void TestConstructorShouldSetProperties()
        {
            int id = 1;
            string user = "Jesé";
            string team = "Team 1";
            var info = new PlayerInformation(id, user, team);
            Assert.AreEqual(user, info.Username);
        }

        [TestMethod]
        public void TestConstructorShouldInitializeHand()
        {
            var info = new PlayerInformation(1, "A", "B");
            var hand = info.Hand;
            Assert.IsNotNull(hand, "The card hand must be initialized empty, not null");
        }

        [TestMethod]
        public void TestNegativeIDShouldStoreValue()
        {
            int negId = -99;
            var info = new PlayerInformation(negId, "User", "Team 1");
            Assert.AreEqual(negId, info.PlayerID);
        }

        [TestMethod]
        public void TestSetTeamShouldUpdateValue()
        {
            var info = new PlayerInformation(1, "User", "Team 1");
            info.Team = "Team 2";
            Assert.AreEqual("Team 2", info.Team);
        }

        [TestMethod]
        public void TestNullUsernameShouldStoreNull()
        {
            string nullUser = null;
            var info = new PlayerInformation(1, nullUser, "Team 1");
            Assert.IsNull(info.Username);
        }
    }
}
