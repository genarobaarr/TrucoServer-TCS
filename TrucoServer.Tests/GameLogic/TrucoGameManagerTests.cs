using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Ranking;

namespace TrucoServer.Tests.GameLogic
{
    [TestClass]
    public class TrucoGameManagerTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<IUserStatsService> mockStats;
        private Mock<DbSet<Match>> mockMatchSet;
        private Mock<DbSet<MatchPlayer>> mockMatchPlayerSet;
        private Mock<DbSet<User>> mockUserSet;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockStats = new Mock<IUserStatsService>();
            mockMatchSet = new Mock<DbSet<Match>>();
            mockMatchPlayerSet = new Mock<DbSet<MatchPlayer>>();
            mockUserSet = new Mock<DbSet<User>>();

            mockContext.Setup(m => m.Match).Returns(mockMatchSet.Object);
            mockContext.Setup(m => m.MatchPlayer).Returns(mockMatchPlayerSet.Object);
            mockContext.Setup(m => m.User).Returns(mockUserSet.Object);
        }

        [TestMethod]
        public void TestConstructorNullContextThrowsArgumentNullException()
        {
            baseDatosTrucoEntities nullContext = null;
            Assert.ThrowsException<ArgumentNullException>(() => new TrucoGameManager(nullContext, mockStats.Object));
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseNewMatchReturnsNewId()
        {
            var manager = new TrucoGameManager(mockContext.Object, mockStats.Object);
            var players = new List<PlayerInformation>();
            var data = new List<Match>().AsQueryable();
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.Provider).Returns(data.Provider);
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.Expression).Returns(data.Expression);
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            manager.SaveMatchToDatabase("CODE", 1, players);
            mockMatchSet.Verify(m => m.Add(It.IsAny<Match>()), Times.Once());
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseExistingInProgressReturnsExistingId()
        {
            var manager = new TrucoGameManager(mockContext.Object, mockStats.Object);
            int existingId = 99;
            
            var data = new List<Match>
            {
                new Match 
                { 
                    matchID = existingId, 
                    lobbyID = 99, 
                    status = "InProgress" 
                }
            }.AsQueryable();

            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.Provider).Returns(data.Provider);
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.Expression).Returns(data.Expression);
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockMatchSet.As<IQueryable<Match>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            int result = manager.SaveMatchToDatabase("CODE", 99, null);
            Assert.AreEqual(existingId, result);
        }

        [TestMethod]
        public void TestSaveMatchResultUpdatesMatchStatus()
        {
            var manager = new TrucoGameManager(mockContext.Object, mockStats.Object);
            
            var match = new Match 
            {
                matchID = 1, 
                status = "InProgress" 
            };

            var outcome = new MatchOutcome
            {
                WinnerTeam = "Team 1", 
                WinnerScore = 30,
                LoserScore = 15
            };

            mockMatchSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns(match);
            manager.SaveMatchResult(1, outcome);
            Assert.AreEqual("Finished", match.status);
        }

        [TestMethod]
        public void TestSaveMatchResultNullOutcomeThrowsExceptionInternal()
        {
            var manager = new TrucoGameManager(mockContext.Object, mockStats.Object);
            manager.SaveMatchResult(1, null);

            mockContext.Verify(c => c.SaveChanges(), Times.Never());
        }
    }
}
