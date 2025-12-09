using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class MatchStarterTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<ILobbyCoordinator> mockCoordinator;
        private Mock<ILobbyRepository> mockRepo;
        private MatchStarter starter;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockCoordinator = new Mock<ILobbyCoordinator>();
            mockRepo = new Mock<ILobbyRepository>();

            var deps = new MatchStarterDependencies
            {
                Context = mockContext.Object,
                Coordinator = mockCoordinator.Object,
                Repository = mockRepo.Object,
                GameRegistry = new Mock<IGameRegistry>().Object,
                GameManager = new Mock<IGameManager>().Object,
                Shuffler = new Mock<IDeckShuffler>().Object,
                ParticipantBuilder = new GamePlayerBuilder(mockContext.Object, mockCoordinator.Object)
            };

            starter = new MatchStarter(deps);
        }

        [TestMethod]
        public void TestValidateMatchStartReturnsFalseIfLobbyNotFound()
        {
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out It.Ref<int>.IsAny)).Returns(false);
            mockRepo.Setup(r => r.FindLobbyByMatchCode("CODE", true)).Returns((Lobby)null);
            var result = starter.ValidateMatchStart("CODE");
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void TestGetOwnerUsernameReturnsNullIfLobbyNotFound()
        {
            mockCoordinator.Setup(c => c.TryGetLobbyIdFromCode("CODE", out It.Ref<int>.IsAny)).Returns(false);
            string owner = starter.GetOwnerUsername("CODE");
            Assert.IsNull(owner);
        }
    }
}