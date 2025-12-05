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
    public class TeamDeterminationOptionsTests
    {
        [TestMethod]
        public void TestMaxPlayersSetPositiveValueReturnsValue()
        {
            var options = new TeamDeterminationOptions();
            int max = 2;
            options.MaxPlayers = max;
            Assert.AreEqual(max, options.MaxPlayers);
        }

        [TestMethod]
        public void TestTeam1CountSetIntegerReturnsInteger()
        {
            var options = new TeamDeterminationOptions();
            int count = 1;
            options.Team1Count = count;
            Assert.AreEqual(count, options.Team1Count);
        }
    }
}
