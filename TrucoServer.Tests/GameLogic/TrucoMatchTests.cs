using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        private MockGameManager mockGameManager;
        private MockTrucoDeck mockDeck;
        private Dictionary<int, ITrucoCallback> mockCallbacks;
        private List<PlayerInformation> players;

        [TestInitialize]
        public void Setup()
        {
            mockGameManager = new MockGameManager();
            mockDeck = new MockTrucoDeck();
            mockCallbacks = new Dictionary<int, ITrucoCallback>();

            players = new List<PlayerInformation>
        {
            new PlayerInformation(1, "Player1", "Team 1"),
            new PlayerInformation(2, "Player2", "Team 2")
        };

            mockCallbacks.Add(1, new MockTrucoCallback());
            mockCallbacks.Add(2, new MockTrucoCallback());
        }

        [TestMethod]
        public void TestConstructorInitializationShouldSetCorrectLobbyId()
        {
            var match = new TrucoMatch("CODE123", 1, players, mockCallbacks, mockDeck, mockGameManager);
            Assert.AreEqual(1, match.LobbyID);
        }

        [TestMethod]
        public void TestConstructorInitializationShouldCallSaveMatch()
        {
            var match = new TrucoMatch("CODE123", 1, players, mockCallbacks, mockDeck, mockGameManager);
            Assert.IsTrue(mockGameManager.SaveMatchCalled);
        }

        [TestMethod]
        public void TestConstructorInitializationShouldSetStateToDeal()
        {
            var match = new TrucoMatch("CODE123", 1, players, mockCallbacks, mockDeck, mockGameManager);
            Assert.AreEqual(GameState.Deal, match.CurrentState);
        }

        [TestMethod]
        public void TestStartNewHandShouldSetStateToEnvido()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            Assert.AreEqual(GameState.Envido, match.CurrentState);
        }

        [TestMethod]
        public void TestStartNewHandShouldDealCardsToPlayer1()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            Assert.AreEqual(3, players[0].Hand.Count);
        }

        [TestMethod]
        public void TestStartNewHandShouldDealCardsToPlayer2()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            Assert.AreEqual(3, players[1].Hand.Count);
        }

        [TestMethod]
        public void TestPlayCardValidTurnShouldReturnTrue()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            string cardToPlay = "sword_1";

            bool result = match.PlayCard(1, cardToPlay);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestPlayCardValidTurnShouldRemoveCardFromHand()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            string cardToPlay = "sword_1";

            match.PlayCard(1, cardToPlay);

            Assert.AreEqual(2, players[0].Hand.Count);
        }

        [TestMethod]
        public void TestPlayCardWrongTurnShouldReturnFalse()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();

            bool result = match.PlayCard(2, "gold_7");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestCallTrucoValidStateShouldReturnTrue()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();

            bool result = match.CallTruco(1, "Truco");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCallTrucoValidStateShouldUpdateGameState()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();

            match.CallTruco(1, "Truco");

            Assert.AreEqual(GameState.Truco, match.CurrentState);
        }

        [TestMethod]
        public void TestRespondToCallQuieroShouldAcceptBetAndContinue()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            match.CallTruco(1, "Truco");

            match.RespondToCall(2, "Quiero");

            Assert.AreEqual(TrucoBet.Truco, match.TrucoBetValue);
        }

        [TestMethod]
        public void TestRespondToCallNoQuieroShouldAwardPointsToOpponent()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            match.CallTruco(1, "Truco");

            match.RespondToCall(2, "NoQuiero");

            Assert.AreEqual(1, match.Team1Score);
        }

        [TestMethod]
        public void TestCallEnvidoAfterCardsPlayedShouldReturnFalse()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();
            match.PlayCard(1, "sword_1");

            bool result = match.CallEnvido(2, "Envido");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestCallFlorPlayerWithoutFlorShouldReturnFalse()
        {
            var match = new TrucoMatch("CODE", 1, players, mockCallbacks, mockDeck, mockGameManager);
            match.StartNewHand();

            bool result = match.CallFlor(1, "Flor");

            Assert.IsFalse(result);
        }
    }

    public class MockGameManager : IGameManager
    {
        public bool SaveMatchCalled { get; private set; } = false;

        public int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformation> players)
        {
            SaveMatchCalled = true;
            return 999;
        }

        public void SaveMatchResult(int matchId, string winnerTeam, int winnerScore, int loserScore) { }
    }

    public class MockTrucoDeck : ITrucoDeck
    {
        public int RemainingCards => 40;

        public List<TrucoCard> DealHand()
        {
            return new List<TrucoCard>
            {
                new TrucoCard(Rank.Uno, Suit.Sword),
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Tres, Suit.Club)
            };
        }

        public TrucoCard DrawCard() { return new TrucoCard(Rank.Cuatro, Suit.Gold); }
        public void Reset() { }
        public void Shuffle() { }
    }

    public class MockTrucoCallback : ITrucoCallback
    {

        public void ReceiveCards(TrucoCard[] hand) { }

        public void NotifyCardPlayed(string playerName, string cardFileName, bool isLastCardOfRound) { }

        public void NotifyTurnChange(string nextPlayerName) { }

        public void NotifyScoreUpdate(int team1Score, int team2Score) { }

        public void NotifyTrucoCall(string callerName, string betName, bool needsResponse) { }

        public void NotifyResponse(string responderName, string response, string newBetState) { }

        public void NotifyRoundEnd(string winnerName, int team1Score, int team2Score) { }

        public void NotifyEnvidoCall(string callerName, string betName, bool needsResponse) { }

        public void NotifyEnvidoResult(string winnerName, int score, int totalPointsAwarded) { }

        public void NotifyFlorCall(string callerName, string betName, bool needsResponse) { }

        public void OnMatchEnded(string matchCode, string winner) { }

        public void OnPlayerJoined(string matchCode, string player) { }

        public void OnPlayerLeft(string matchCode, string player) { }

        public void OnCardPlayed(string matchCode, string player, string card) { }

        public void OnChatMessage(string matchCode, string player, string message) { }

        public void OnMatchStarted(string matchCode, List<PlayerInfo> players) { }

        public void OnFriendRequestReceived(string fromUser) { }

        public void OnFriendRequestAccepted(string fromUser) { }

        public void Ping() { }
    }
}
