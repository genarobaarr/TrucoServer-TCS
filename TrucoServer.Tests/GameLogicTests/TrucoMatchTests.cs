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

namespace TrucoServer.Tests.GameLogic
{
    [TestClass]
    public class TrucoMatchTests
    {
        private Mock<IGameManager> mockGameManager;
        private Mock<ITrucoDeck> mockDeck;
        private TrucoMatchContext context;
        private List<PlayerInformationWithConstructor> players;
        private Dictionary<int, ITrucoCallback> callbacks;

        private const int LOBBY_ID = 1;
        private const int PLAYER_INFORMATION_1 = 1;
        private const int PLAYER_INFORMATION_2 = 2;

        [TestInitialize]
        public void Setup()
        {
            mockGameManager = new Mock<IGameManager>();
            mockDeck = new Mock<ITrucoDeck>();
            
            players = new List<PlayerInformationWithConstructor>
            {
                new PlayerInformationWithConstructor(PLAYER_INFORMATION_1, "P1", "Team 1"),
                new PlayerInformationWithConstructor(PLAYER_INFORMATION_2, "P2", "Team 2")
            };
            
            callbacks = new Dictionary<int, ITrucoCallback>
            {
                { 
                    PLAYER_INFORMATION_1, 
                    new Mock<ITrucoCallback>().Object 
                },

                { 
                    PLAYER_INFORMATION_2,
                    new Mock<ITrucoCallback>().Object 
                }
            };

            mockGameManager.Setup(gm => gm.SaveMatchToDatabase(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<PlayerInformationWithConstructor>>()))
                            .Returns(100);

            context = new TrucoMatchContext
            {
                MatchCode = "TEST-MATCH",
                LobbyId = LOBBY_ID,
                Players = players,
                Callbacks = callbacks,
                Deck = mockDeck.Object,
                GameManager = mockGameManager.Object
            };
        }

        [TestMethod]
        public void TestConstructorInitializesMatchCorrectly()
        {
            var match = new TrucoMatch(context);
            Assert.AreEqual("TEST-MATCH", match.MatchCode);
        }

        [TestMethod]
        public void TestStartNewHandChangesStateToEnvido()
        {
            var match = new TrucoMatch(context);
            mockDeck.Setup(d => d.DealHand()).Returns(new List<TrucoCard>());
            match.StartNewHand();
            Assert.AreEqual(GameState.Envido, match.CurrentState);
        }

        [TestMethod]
        public void TestCallTrucoValidUpdateStateToTruco()
        {
            var match = new TrucoMatch(context);
            match.StartNewHand();
            bool result = match.CallTruco(1, "Truco");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestPlayCardFailsIfTurnIsWrong()
        {
            var match = new TrucoMatch(context);
            match.StartNewHand();
            bool result = match.PlayCard(2, "sw_1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestAbortMatchEndsTheGame()
        {
            var match = new TrucoMatch(context);
            match.AbortMatch("P1");
            mockGameManager.Verify(gm => gm.SaveMatchResult(It.IsAny<int>(), It.IsAny<MatchOutcome>()), Times.Once);
        }
    }
}
