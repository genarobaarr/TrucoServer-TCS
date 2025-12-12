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
        private Mock<IUserStatisticsService> mockStats;
        private Mock<DbSet<User>> mockUserSet;

        private const int LOBBY_ID = 1;
        private const int WINER_SCORE = 30;
        private const int LOSSER_SCORE = 30;
        private const int MATCH_ID = 10;
        private const int SECOND_MATCH_ID = 99;
        private const int SECOND_LOBBY_ID = 50;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockStats = new Mock<IUserStatisticsService>();
            mockUserSet = new Mock<DbSet<User>>();
            mockContext.Setup(m => m.User).Returns(mockUserSet.Object);
        }

        private static Mock<DbSet<T>> GetMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            mockSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => sourceList.Add(s));

            return mockSet;
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
            var players = new List<PlayerInformationWithConstructor>();
            int lobbyId = LOBBY_ID;
            var matchList = new List<Match>();
            
            var lobbyList = new List<Lobby> 
            { 
                new Lobby
                {
                    lobbyID = lobbyId
                } 
            };

            var mockMatchSet = GetMockDbSet(matchList);
            var mockLobbySet = GetMockDbSet(lobbyList);

            mockLobbySet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((object[] ids) => lobbyList.FirstOrDefault(l => l.lobbyID == (int)ids[0]));
            mockContext.Setup(c => c.Match).Returns(mockMatchSet.Object);
            mockContext.Setup(c => c.Lobby).Returns(mockLobbySet.Object);
            manager.SaveMatchToDatabase("CODE", lobbyId, players);
            mockMatchSet.Verify(m => m.Add(It.IsAny<Match>()), Times.Once());
        }

        [TestMethod]
        public void TestSaveMatchToDatabaseExistingInProgressReturnsExistingId()
        {
            var manager = new TrucoGameManager(mockContext.Object, mockStats.Object);
            int existingId = SECOND_MATCH_ID;
            int lobbyId = SECOND_LOBBY_ID;

            var matchList = new List<Match>
            {
                new Match 
                {
                    matchID = existingId, 
                    lobbyID = lobbyId, 
                    status = "InProgress" 
                }
            };

            var lobbyList = new List<Lobby> 
            { 
                new Lobby 
                { 
                    lobbyID = lobbyId
                } 
            };

            var mockMatchSet = GetMockDbSet(matchList);
            var mockLobbySet = GetMockDbSet(lobbyList);

            mockLobbySet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((object[] ids) => lobbyList.FirstOrDefault(l => l.lobbyID == (int)ids[0]));
            mockContext.Setup(c => c.Match).Returns(mockMatchSet.Object);
            mockContext.Setup(c => c.Lobby).Returns(mockLobbySet.Object);

            int result = manager.SaveMatchToDatabase("CODE", lobbyId, null);
            Assert.AreEqual(existingId, result);
        }

        [TestMethod]
        public void TestSaveMatchResultUpdatesMatchStatus()
        {
            var manager = new TrucoGameManager(mockContext.Object, mockStats.Object);
            int matchId = MATCH_ID;

            var match = new Match 
            { 
                matchID = matchId,
                status = "InProgress"
            };

            var matchList = new List<Match> 
            {
                match
            };

            var matchPlayerList = new List<MatchPlayer>();
            var mockMatchPlayerSet = GetMockDbSet(matchPlayerList);

            var outcome = new MatchOutcome
            {
                WinnerTeam = "Team 1",
                WinnerScore = WINER_SCORE,
                LoserScore = LOSSER_SCORE
            };

            var mockMatchSet = GetMockDbSet(matchList);
            mockMatchSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((object[] ids) => matchList.FirstOrDefault(m => m.matchID == (int)ids[0]));
            mockContext.Setup(c => c.Match).Returns(mockMatchSet.Object);
            mockContext.Setup(c => c.MatchPlayer).Returns(mockMatchPlayerSet.Object);
            manager.SaveMatchResult(matchId, outcome);

            Assert.AreEqual("Finished", match.status);
            mockContext.Verify(c => c.SaveChanges(), Times.Once());
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
