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
    public class TeamCountResultTests
    {
        [TestMethod]
        public void TestTeam1CountSetPositiveReturnsPositive()
        {
            var result = new TeamCountsResult();
            int count = 5;
            result.Team1Count = count;
            Assert.AreEqual(count, result.Team1Count);
        }

        [TestMethod]
        public void TestTeam2CountSetPositiveReturnsPositive()
        {
            var result = new TeamCountsResult();
            int count = 3;
            result.Team2Count = count;
            Assert.AreEqual(count, result.Team2Count);
        }

        [TestMethod]
        public void TestTeam1CountSetZeroReturnsZero()
        {
            var result = new TeamCountsResult();
            result.Team1Count = 0;
            Assert.AreEqual(0, result.Team1Count);
        }

        [TestMethod]
        public void TestTeam2CountSetNegativeReturnsNegative()
        {
            var result = new TeamCountsResult();
            int negative = -5;
            result.Team2Count = negative;
            Assert.AreEqual(negative, result.Team2Count);
        }

        [TestMethod]
        public void TestConstructorInstanceIsNotNull()
        {
            var result = new TeamCountsResult();
            Assert.IsNotNull(result);
        }
    }
}
