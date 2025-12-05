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
    public class PublicLobbyInfoTests
    {
        [TestMethod]
        public void TestMatchNameSetValidStringReturnsString()
        {
            var info = new PublicLobbyInfo();
            string name = "Fun Room";
            info.MatchName = name;
            Assert.AreEqual(name, info.MatchName);
        }

        [TestMethod]
        public void TestCurrentPlayersSetZeroReturnsZero()
        {
            var info = new PublicLobbyInfo();
            int count = 0;
            info.CurrentPlayers = count;
            Assert.AreEqual(0, info.CurrentPlayers);
        }

        [TestMethod]
        public void TestMaxPlayersSetPositiveValueReturnsValue()
        {
            var info = new PublicLobbyInfo();
            int max = 4;
            info.MaxPlayers = max;
            Assert.AreEqual(max, info.MaxPlayers);
        }
    }
}
