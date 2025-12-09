using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class LobbyRepositoryTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<DbSet<Lobby>> mockLobbySet;
        private LobbyRepository repository;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockLobbySet = new Mock<DbSet<Lobby>>();
            mockContext.Setup(c => c.Lobby).Returns(mockLobbySet.Object);
            repository = new LobbyRepository(mockContext.Object);
        }

        [TestMethod]
        public void TestCreateNewLobbyAddsEntityToContext()
        {
            var host = new User
            { 
                userID = 1
            };

            var options = new LobbyCreationOptions
            {
                Host = host,
                MaxPlayers = 2,
                Status = "Public"
            };

            repository.CreateNewLobby(options);
            mockLobbySet.Verify(m => m.Add(It.IsAny<Lobby>()), Times.Once);
        }

        [TestMethod]
        public void TestResolveVersionIdReturnsZeroIfNoVersionFound()
        {
            var emptyVersions = new List<Versions>().AsQueryable();
            var mockVersionSet = new Mock<DbSet<Versions>>();

            mockVersionSet.As<IQueryable<Versions>>().Setup(m => m.Provider).Returns(emptyVersions.Provider);
            mockVersionSet.As<IQueryable<Versions>>().Setup(m => m.Expression).Returns(emptyVersions.Expression);
            mockVersionSet.As<IQueryable<Versions>>().Setup(m => m.ElementType).Returns(emptyVersions.ElementType);
            mockVersionSet.As<IQueryable<Versions>>().Setup(m => m.GetEnumerator()).Returns(emptyVersions.GetEnumerator());

            mockContext.Setup(c => c.Versions).Returns(mockVersionSet.Object);
            int versionId = repository.ResolveVersionId(2);
            Assert.AreEqual(0, versionId);
        }
    }
}

