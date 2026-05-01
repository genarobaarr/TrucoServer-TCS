using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using TrucoServer;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Services;

namespace TrucoServer.Tests.ServicesTests
{
    [TestClass]
    public class TrucoTournamentServiceImplementationTests
    {
        private Mock<baseDatosTrucoEntities> mockContainer;
        private TrucoTournamentServiceImplementation tournamentService;
        private Mock<DbSet<Tournaments>> mockTournamentsSet;
        private Mock<DbSet<TournamentBrackets>> mockBracketsSet;

        [TestInitialize]
        public void Setup()
        {
            mockContainer = new Mock<baseDatosTrucoEntities>();
            mockTournamentsSet = new Mock<DbSet<Tournaments>>();
            mockBracketsSet = new Mock<DbSet<TournamentBrackets>>();

            mockContainer.Setup(c => c.Tournaments).Returns(mockTournamentsSet.Object);
            mockContainer.Setup(c => c.TournamentBrackets).Returns(mockBracketsSet.Object);

            tournamentService = new TrucoTournamentServiceImplementation(mockContainer.Object);
        }

        private Mock<DbSet<T>> SetupMockDbSet<T>(List<T> sourceList) where T : class
        {
            Mock<DbSet<T>> mockSet = new Mock<DbSet<T>>();
            IQueryable<T> queryableList = sourceList.AsQueryable();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableList.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableList.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableList.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryableList.GetEnumerator());

            return mockSet;
        }

        [TestMethod]
        public void GetTournamentTree_ReturnsOrderedBrackets()
        {
            List<TournamentBrackets> fakeBrackets = new List<TournamentBrackets>
            {
                new TournamentBrackets { Id = 1, TournamentId = 10, Round = 2, Position = 0 },
                new TournamentBrackets { Id = 2, TournamentId = 10, Round = 1, Position = 1 },
                new TournamentBrackets { Id = 3, TournamentId = 10, Round = 1, Position = 0 }
            };

            mockBracketsSet = SetupMockDbSet(fakeBrackets);
            mockContainer.Setup(c => c.TournamentBrackets).Returns(mockBracketsSet.Object);

            List<BracketDTO> result = null;

            try
            {
                result = tournamentService.GetTournamentTree(10);
            }
            catch (Exception ex)
            {
                Assert.Fail("Se lanzó una excepción inesperada: " + ex.Message);
            }

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            if (result.Count == 3)
            {
                Assert.AreEqual(1, result[0].Round);
                Assert.AreEqual(0, result[0].Position);

                Assert.AreEqual(1, result[1].Round);
                Assert.AreEqual(1, result[1].Position);

                Assert.AreEqual(2, result[2].Round);
            }
        }

        [TestMethod]
        public void GetTournamentTree_NoBracketsFound_ReturnsEmptyList()
        {
            List<TournamentBrackets> emptyBrackets = new List<TournamentBrackets>();
            mockBracketsSet = SetupMockDbSet(emptyBrackets);
            mockContainer.Setup(c => c.TournamentBrackets).Returns(mockBracketsSet.Object);

            List<BracketDTO> result = null;

            try
            {
                result = tournamentService.GetTournamentTree(99);
            }
            catch (Exception ex)
            {
                Assert.Fail("Se lanzó una excepción inesperada: " + ex.Message);
            }

            Assert.IsNotNull(result);

            if (result != null)
            {
                Assert.AreEqual(0, result.Count);
            }
        }

        [TestMethod]
        public void GetAvailableTournaments_ReturnsOnlyWaitingStatus()
        {
            List<Tournaments> fakeTournaments = new List<Tournaments>
            {
                new Tournaments { Id = 1, Name = "Torneo A", Status = "Waiting", Capacity = 8 },
                new Tournaments { Id = 2, Name = "Torneo B", Status = "InProgress", Capacity = 4 },
                new Tournaments { Id = 3, Name = "Torneo C", Status = "Finished", Capacity = 8 }
            };

            mockTournamentsSet = SetupMockDbSet(fakeTournaments);
            mockContainer.Setup(c => c.Tournaments).Returns(mockTournamentsSet.Object);

            List<TournamentDTO> result = null;

            try
            {
                result = tournamentService.GetAvailableTournaments();
            }
            catch (Exception ex)
            {
                Assert.Fail("Se lanzó una excepción inesperada: " + ex.Message);
            }

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Torneo A", result[0].Name);
        }

        [TestMethod]
        public void GetAvailableTournaments_DatabaseError_ThrowsFaultException()
        {
            mockContainer.Setup(c => c.Tournaments).Throws(new Exception("Database connection lost"));

            try
            {
                tournamentService.GetAvailableTournaments();
                Assert.Fail("Se esperaba una FaultException.");
            }
            catch (FaultException<CustomFault> fault)
            {
                Assert.IsNotNull(fault);
            }
            catch (Exception ex)
            {
                Assert.Fail("Se esperaba FaultException<CustomFault>, pero se recibió: " + ex.GetType().Name);
            }
        }
    }
}