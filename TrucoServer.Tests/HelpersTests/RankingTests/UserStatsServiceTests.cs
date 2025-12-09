using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Ranking;

namespace TrucoServer.Tests.HelpersTests.RankingTests
{
    [TestClass]
    public class UserStatsServiceTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<DbSet<User>> mockUserSet;
        private UserStatsService service;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockUserSet = new Mock<DbSet<User>>();
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);

            service = new UserStatsService(mockContext.Object);
        }

        [TestMethod]
        public void TestUpdateUserStatsIncrementsWinsCountForWinner()
        {
            var user = new User 
            {
                userID = 1,
                wins = 10, 
                losses = 5
            };
            
            SetupMockUser(user);
            service.UpdateUserStats(1, true);
            Assert.AreEqual(11, user.wins);
        }

        [TestMethod]
        public void TestUpdateUserStatsDoesNotChangeLossesForWinner()
        {
            var user = new User 
            {
                userID = 1,
                wins = 10, 
                losses = 5 
            };
            
            SetupMockUser(user);
            service.UpdateUserStats(1, true);
            Assert.AreEqual(5, user.losses);
        }

        [TestMethod]
        public void TestUpdateUserStatsIncrementsLossesCountForLoser()
        {
            var user = new User 
            { 
                userID = 2,
                wins = 0, 
                losses = 0
            };
           
            SetupMockUser(user);
            service.UpdateUserStats(2, false);
            Assert.AreEqual(1, user.losses);
        }

        private void SetupMockUser(User user)
        {
            var data = new List<User> { user }.AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        }
    }
}
