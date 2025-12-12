using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using TrucoServer.Helpers.Ranking;

namespace TrucoServer.Tests.HelpersTests.RankingTests
{
    [TestClass]
    public class UserStatisticsServiceTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private UserStatisticsService service;

        private const int USER_ID = 1;
        private const int WINS = 10;
        private const int LOSSES = 5;
        private const int SECOND_USER_ID = 2;
        private const int ZERO_WINS_OR_LOSSES = 0;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            service = new UserStatisticsService(mockContext.Object);
        }

        [TestMethod]
        public void TestUpdateUserStatsIncrementsWinsCountForWinner()
        {
            var user = new User
            {
                userID = USER_ID,
                wins = WINS,
                losses = LOSSES
            };

            var mockSet = GetMockDbSet(new List<User> 
            {
                user 
            });

            mockContext.Setup(c => c.User).Returns(mockSet.Object);
            service.UpdateUserStats(USER_ID, true);
            Assert.AreEqual(11, user.wins);
        }

        [TestMethod]
        public void TestUpdateUserStatsDoesNotChangeLossesForWinner()
        {
            var user = new User
            {
                userID = USER_ID,
                wins = WINS,
                losses = LOSSES
            };

            var mockSet = GetMockDbSet(new List<User> 
            {
                user 
            });

            mockContext.Setup(c => c.User).Returns(mockSet.Object);
            service.UpdateUserStats(USER_ID, true);
            Assert.AreEqual(5, user.losses);
        }

        [TestMethod]
        public void TestUpdateUserStatsIncrementsLossesCountForLoser()
        {
            var user = new User
            {
                userID = SECOND_USER_ID,
                wins = ZERO_WINS_OR_LOSSES,
                losses = ZERO_WINS_OR_LOSSES
            };

            var mockSet = GetMockDbSet(new List<User>
            { 
                user 
            });

            mockContext.Setup(c => c.User).Returns(mockSet.Object);
            service.UpdateUserStats(SECOND_USER_ID, false);
            Assert.AreEqual(1, user.losses);
        }

        private static Mock<DbSet<T>> GetMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return mockSet;
        }
    }
}