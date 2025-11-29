using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public enum GameState { 
        Deal, 
        Envido, 
        Flor,
        Truco, 
        HandEnd, 
        MatchEnd 
    }

    public enum TrucoBet 
    { 
        None, 
        Truco, 
        Retruco, 
        ValeCuatro 
    }
    
    public enum EnvidoBet 
    { 
        None, 
        Envido, 
        RealEnvido, 
        FaltaEnvido 
    }

    public enum FlorBet
    {
        None,
        Flor,
        ContraFlor
    }

    public class TrucoMatch
    {
        private const int MAX_SCORE = 30;
        private const int CARDS_IN_HAND = 3;
        private const int MAX_ROUNDS = 3;
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";
        private const string DRAW_STATUS = "Draw";
        private const string NO_QUIERO_STATUS = "NoQuiero";
        private const string QUIERO_STATUS = "Quiero";
        private const string AL_MAZO = "Me voy al mazo";
        private const string FLOR_BET = "Flor";
        private const string CONTRA_FLOR_BET = "ContraFlor";

        private const int INITIAL_SCORE = 0;
        private const int CURRENT_ROUND = 0;
        private const int CURRENT_ENVIDO_POINT = 0;
        private const int INDEX_VALUE = 0;
        private const int CURRENT_FLOR_SCORE = 0;
        private const int CURRENT_FLOR_POINT = 0;
        private const int POINTS_FLOR_DIRECT = 3;
        private const int POINTS_CONTRA_FLOR = 4;

        public string MatchCode { get; private set; }
        public int DbMatchId { get; private set; }
        public int LobbyID { get; private set; }
        public List<PlayerInformation> Players { get; private set; }
        public Dictionary<int, ITrucoCallback> PlayerCallbacks { get; private set; }
        public int Team1Score { get; private set; }
        public int Team2Score { get; private set; }
        public GameState CurrentState { get; private set; }

        private readonly ITrucoDeck deck;
        private readonly IGameManager gameManager;
        private readonly Dictionary<int, List<TrucoCard>> playerHands;
        private readonly Dictionary<int, List<TrucoCard>> playedCards;
        private readonly Dictionary<int, TrucoCard> cardsOnTable;
        private readonly object matchLock = new object();

        private string[] roundWinners;
        private int currentRound;
        private int handStartingPlayerIndex;
        private int turnIndex;
        
        public TrucoBet TrucoBetValue { get; private set; }
        private int? bettingPlayerId;
        private int? waitingForResponseToId;
        private TrucoBet proposedBetValue;

        public EnvidoBet EnvidoBetValue { get; private set; }
        private EnvidoBet proposedEnvidoBet;
        private int currentEnvidoPoints;
        private int? envidoBettorId;
        private int? waitingForEnvidoResponseId;
        private bool envidoWasPlayed;
        private bool matchEnded = false;
        private Dictionary<int, int> playerEnvidoScores;
        
        public FlorBet FlorBetValue { get; private set; }
        private int currentFlorPoints;
        private int? florBettorId;
        private int? waitingForFlorResponseId;
        private bool florWasPlayed;
        private Dictionary<int, int> playerFlorScores;
        
        public TrucoMatch(string matchCode, int lobbyId, List<PlayerInformation> players, Dictionary<int, 
            ITrucoCallback> callbacks, ITrucoDeck deck, IGameManager gameManager)
        {
            try
            {
                this.MatchCode = matchCode;
                this.LobbyID = lobbyId;
                this.DbMatchId = gameManager.SaveMatchToDatabase(MatchCode, lobbyId, players);

                this.Players = players;
                this.PlayerCallbacks = callbacks;
                this.deck = deck;
                this.gameManager = gameManager;

                this.playerHands = players.ToDictionary(p => p.PlayerID, p => new List<TrucoCard>());
                this.playedCards = players.ToDictionary(p => p.PlayerID, p => new List<TrucoCard>());
                this.cardsOnTable = new Dictionary<int, TrucoCard>();
                this.roundWinners = new string[MAX_ROUNDS];
                this.Team1Score = INITIAL_SCORE;
                this.Team2Score = INITIAL_SCORE;
                this.CurrentState = GameState.Deal;
                this.TrucoBetValue = TrucoBet.None;
                this.handStartingPlayerIndex = 0;
                this.turnIndex = 0;

                gameManager.SaveMatchToDatabase(MatchCode, LobbyID, players);
            }
            catch (ArgumentNullException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(TrucoMatch));
                throw;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(TrucoMatch));
                throw;
            }
        }

        public void StartNewHand()
        {
            try
            {
                CurrentState = GameState.Envido;
                deck.Reset();
                deck.Shuffle();
                playerHands.Clear();
                cardsOnTable.Clear();

                foreach (var key in playedCards.Keys.ToList())
                {
                    playedCards[key].Clear();
                }

                roundWinners = new string[MAX_ROUNDS];
                currentRound = CURRENT_ROUND;

                TrucoBetValue = TrucoBet.None;
                bettingPlayerId = null;
                waitingForResponseToId = null;

                handStartingPlayerIndex = (handStartingPlayerIndex + 1) % Players.Count;
                turnIndex = handStartingPlayerIndex;

                EnvidoBetValue = EnvidoBet.None;
                proposedEnvidoBet = EnvidoBet.None;
                currentEnvidoPoints = CURRENT_ENVIDO_POINT;
                envidoBettorId = null;
                waitingForEnvidoResponseId = null;
                envidoWasPlayed = false;

                FlorBetValue = FlorBet.None;
                currentFlorPoints = CURRENT_FLOR_POINT;
                florBettorId = null;
                waitingForFlorResponseId = null;
                florWasPlayed = false;

                foreach (var player in Players)
                {
                    var hand = deck.DealHand();
                    playerHands[player.PlayerID] = hand;
                    player.Hand = hand;

                    NotifyPlayer(player.PlayerID, callback => callback.ReceiveCards(hand.ToArray()));
                }

                playerEnvidoScores = Players.ToDictionary(
                    p => p.PlayerID,
                    p => TrucoRules.CalculateEnvidoScore(playerHands[p.PlayerID])
                );

                playerFlorScores = Players.ToDictionary(
                    p => p.PlayerID,
                    p => TrucoRules.HasFlor(playerHands[p.PlayerID])
                        ? TrucoRules.CalculateFlorScore(playerHands[p.PlayerID])
                        : -1
                );

                CurrentState = GameState.Envido;
                NotifyTurnChange();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(StartNewHand));
            }

            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogError(ex, nameof(StartNewHand));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(StartNewHand));
            }
        }

        public bool PlayCard(int playerID, string cardFileName)
        {
            try
            {
                if (waitingForFlorResponseId.HasValue)
                {
                    return false;
                }

                if (waitingForEnvidoResponseId.HasValue)
                {
                    return false;
                }

                var player = GetCurrentTurnPlayer();

                if (player.PlayerID != playerID || (CurrentState != GameState.Truco && CurrentState != GameState.Envido && CurrentState != GameState.Flor))
                {
                    return false;
                }

                if (!playerHands.ContainsKey(playerID))
                {
                    return false;
                }

                var hand = playerHands[playerID];
                var cardInHand = hand.FirstOrDefault(c => c.FileName == cardFileName);

                if (cardInHand == null)
                {
                    return false;
                }

                if (waitingForResponseToId.HasValue)
                {
                    return false;
                }

                hand.Remove(cardInHand);
                playedCards[playerID].Add(cardInHand);
                cardsOnTable.Add(playerID, cardInHand);

                bool isLastCardOfRound = (playedCards[playerID].Count == CARDS_IN_HAND);
                NotifyAll(callback => callback.NotifyCardPlayed(player.Username, cardFileName, isLastCardOfRound));

                AdvanceTurn();

                return true;
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(PlayCard));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(PlayCard));
                return false;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogError(ex, nameof(PlayCard));
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(PlayCard));
                return false;
            }
        }

        private void AdvanceTurn()
        {
            if (Players == null || Players.Count == 0)
            {
                LogManager.LogError(new InvalidOperationException("Attempt to advance turn without players"), nameof(AdvanceTurn));
                return;
            }

            if (cardsOnTable == null)
            {
                LogManager.LogError(new InvalidOperationException("The list of cards on the table is null"), nameof(AdvanceTurn));
                return;
            }

            try
            {
                turnIndex = (turnIndex + 1) % Players.Count;

                if (cardsOnTable.Count == Players.Count)
                {
                    ResolveCurrentRound();
                }
                else
                {
                    NotifyTurnChange();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AdvanceTurn));
            }
        }

        private void ResolveCurrentRound()
        {
            try
            {
                var roundWinner = DetermineRoundWinner();

                ProcessRoundCompletion(roundWinner);

                if (CheckHandWinner())
                {
                    ProcessHandCompletion();
                }
                else
                {
                    NotifyTurnChange();
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveCurrentRound));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveCurrentRound));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveCurrentRound));
            }
        }

        public bool CallTruco(int playerID, string betType)
        {
            try
            {
                if (!CanInitiateTrucoCall())
                {
                    return false;
                }

                var caller = Players.FirstOrDefault(p => p.PlayerID == playerID);
                if (caller == null)
                {
                    return false;
                }

                if (!Enum.TryParse(betType, out TrucoBet newBet))
                {
                    return false;
                }

                if (IsInvalidTrucoBetSequence(newBet))
                {
                    return false;
                }

                var opponent = GetOpponentToRespond(caller);
                if (opponent == null)
                {
                    return false;
                }

                CurrentState = GameState.Truco;

                ApplyTrucoCallState(playerID, newBet, opponent.PlayerID);

                NotifyTrucoCall(playerID, betType, opponent.PlayerID);

                return true;
            }
            catch (ArgumentException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(CallTruco));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(CallTruco));
                return false;
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogError(ex, nameof(CallTruco));
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CallTruco));
                return false;
            }
        }

        public void RespondToCall(int playerID, string response)
        {
            try
            {
                if (!waitingForResponseToId.HasValue || waitingForResponseToId.Value != playerID)
                {
                    return;
                }

                var responder = Players.First(p => p.PlayerID == playerID);
                var caller = Players.First(p => p.PlayerID == bettingPlayerId.Value);

                if (response == NO_QUIERO_STATUS)
                {
                    int pointsToAward = GetPointsForBet(TrucoBetValue);
                    NotifyResponse(response, responder.Username, TrucoBetValue.ToString());
                    EndHandWithPoints(caller.Team, pointsToAward);
                }
                else if (response == QUIERO_STATUS)
                {
                    TrucoBetValue = proposedBetValue;
                    NotifyResponse(response, responder.Username, TrucoBetValue.ToString());
                    ResetBetState();
                    NotifyTurnChange();
                }
                else
                {
                    ResetBetState();
                    CallTruco(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(RespondToCall));
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogError(ex, nameof(RespondToCall));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RespondToCall));
            }
        }

        public bool CallEnvido(int playerID, string betType)
        {
            try
            {
                if (envidoWasPlayed)
                {
                    return false;
                }

                if (waitingForEnvidoResponseId.HasValue)
                {
                    return false;
                }

                if (CurrentState != GameState.Envido)
                {
                    return false;
                }

                var caller = Players.FirstOrDefault(p => p.PlayerID == playerID);

                if (caller == null)
                {
                    return false;
                }

                EnvidoBet newBet;

                if (!Enum.TryParse(betType, out newBet))
                {
                    return false;
                }

                var opponent = GetOpponentToRespond(caller);

                if (opponent == null)
                {
                    return false;
                }

                envidoBettorId = playerID;
                waitingForEnvidoResponseId = opponent.PlayerID;
                proposedEnvidoBet = newBet;

                if (newBet == EnvidoBet.FaltaEnvido)
                {
                    currentEnvidoPoints = GetPointsForFaltaEnvido();
                }
                else
                {
                    currentEnvidoPoints += GetPointsForEnvidoBet(newBet);
                }

                NotifyAll(cb => cb.NotifyEnvidoCall(caller.Username, betType, true));
                NotifyPlayer(opponent.PlayerID, cb => cb.NotifyEnvidoCall(caller.Username, betType, true));

                return true;
            }
            catch (ArgumentException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(CallEnvido));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(CallEnvido));
                return false;
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogError(ex, nameof(CallEnvido));
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CallEnvido));
                return false;
            }
        }

        public void RespondToEnvido(int playerID, string response)
        {
            try
            {
                if (!waitingForEnvidoResponseId.HasValue || waitingForEnvidoResponseId.Value != playerID)
                {
                    return;
                }

                var caller = Players.First(p => p.PlayerID == envidoBettorId.Value);
                envidoWasPlayed = true;

                if (response == NO_QUIERO_STATUS)
                {
                    HandleEnvidoNoQuiero(caller);
                }
                else if (response == QUIERO_STATUS)
                {
                    HandleEnvidoQuiero(caller);
                }
                else
                {
                    HandleEnvidoRaise(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(RespondToEnvido));
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogError(ex, nameof(RespondToEnvido));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RespondToEnvido));
            }
        }

        private void ResolveEnvido()
        {
            try
            {
                PlayerInformation envidoWinner = null;
                int highestScore = -1;

                for (int i = 0; i < Players.Count; i++)
                {
                    int playerIndex = (handStartingPlayerIndex + i) % Players.Count;
                    var player = Players[playerIndex];
                    int score = playerEnvidoScores[player.PlayerID];

                    if (score > highestScore)
                    {
                        highestScore = score;
                        envidoWinner = player;
                    }
                }

                if (envidoWinner != null)
                {
                    NotifyAll(cb => cb.NotifyEnvidoResult(envidoWinner.Username, highestScore, currentEnvidoPoints));
                    AwardEnvidoPoints(envidoWinner.Team, currentEnvidoPoints);
                }

                ResetEnvidoState();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveEnvido));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveEnvido));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveEnvido));
            }
        }

        public bool CallFlor(int playerID, string betType)
        {
            try
            {
                bool anyCardPlayed = playedCards.Values.Any(list => list.Count > 0);
                if (anyCardPlayed)
                {
                    return false;
                }

                if (florWasPlayed || waitingForFlorResponseId.HasValue)
                {
                    return false;
                }

                if (CurrentState == GameState.Truco)
                {
                    return false;
                }

                var caller = Players.FirstOrDefault(p => p.PlayerID == playerID);
                if (caller == null)
                {
                    return false;
                }

                if (playerFlorScores[playerID] == -1)
                {
                    LogManager.LogWarn($"[GAME] Player {caller.Username} tried to call Flor without having it.", nameof(CallFlor));
                    return false;
                }

                var opponent = GetOpponentToRespond(caller);
                if (opponent == null)
                {
                    return false;
                }

                bool opponentHasFlor = playerFlorScores[opponent.PlayerID] > -1;

                if (!opponentHasFlor)
                {
                    AwardFlorPoints(caller.Team, POINTS_FLOR_DIRECT);

                    NotifyAll(cb => cb.NotifyFlorCall(caller.Username, FLOR_BET, false));
                    NotifyAll(cb => cb.NotifyEnvidoResult(caller.Username, POINTS_FLOR_DIRECT, POINTS_FLOR_DIRECT));

                    florWasPlayed = true;
                    CurrentState = GameState.Truco;
                    NotifyTurnChange();
                    return true;
                }
                else
                {
                    if (CurrentState == GameState.Envido)
                    {
                        ResetEnvidoState(false);
                    }

                    CurrentState = GameState.Flor;
                    florBettorId = playerID;
                    waitingForFlorResponseId = opponent.PlayerID;
                    FlorBetValue = FlorBet.Flor;

                    NotifyFlorCallHelper(playerID, FLOR_BET, opponent.PlayerID);

                    return true;
                }
            }
            catch (ArgumentException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(CallFlor));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(CallFlor));
                return false;
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogError(ex, nameof(CallFlor));
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CallFlor));
                return false;
            }
        }

        public void RespondToFlor(int playerID, string response)
        {
            try
            {
                if (!waitingForFlorResponseId.HasValue || waitingForFlorResponseId.Value != playerID)
                {
                    return;
                }

                var responder = Players.First(p => p.PlayerID == playerID);


                if (response == CONTRA_FLOR_BET)
                {
                    FlorBetValue = FlorBet.ContraFlor;
                    NotifyResponse(response, responder.Username, FlorBetValue.ToString());

                    ResolveContraFlor();
                }
                else if (response == NO_QUIERO_STATUS)
                {
                    var caller = Players.First(p => p.PlayerID == florBettorId.Value);
                    AwardFlorPoints(caller.Team, POINTS_FLOR_DIRECT);
                    ResetFlorState();
                    CurrentState = GameState.Truco;
                    NotifyTurnChange();
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(RespondToFlor));
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogError(ex, nameof(RespondToFlor));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RespondToFlor));
            }
        }

        private void ResolveFlor()
        {
            try
            {
                PlayerInformation florWinner = null;
                int highestScore = -1;

                for (int i = 0; i < Players.Count; i++)
                {
                    int playerIndex = (handStartingPlayerIndex + i) % Players.Count;
                    var player = Players[playerIndex];
                    int score = playerFlorScores[player.PlayerID];

                    if (score > -1 && score > highestScore)
                    {
                        highestScore = score;
                        florWinner = player;
                    }
                }

                if (florWinner != null)
                {
                    NotifyAll(cb => cb.NotifyEnvidoResult(florWinner.Username, highestScore, currentFlorPoints));
                    AwardEnvidoPoints(florWinner.Team, currentFlorPoints);
                }

                ResetFlorState();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveFlor));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveFlor));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveFlor));
            }
        }

        private void ResolveContraFlor()
        {
            try
            {
                int id1 = florBettorId.Value;
                int id2 = waitingForFlorResponseId.Value;

                int winnerId = DetermineContraFlorWinner(id1, id2);
                var winner = Players.First(p => p.PlayerID == winnerId);

                int score1 = playerFlorScores[id1];
                int score2 = playerFlorScores[id2];
                int winningScore = Math.Max(score1, score2);

                NotifyAll(cb => cb.NotifyEnvidoResult(winner.Username, winningScore, POINTS_CONTRA_FLOR));

                AwardFlorPoints(winner.Team, POINTS_CONTRA_FLOR);

                ResetFlorState();
                CurrentState = GameState.Truco;
                NotifyTurnChange();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveContraFlor));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveContraFlor));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveContraFlor));
            }
        }

        private void HandleEnvidoNoQuiero(PlayerInformation caller)
        {
            int pointsToAward;

            if (proposedEnvidoBet == EnvidoBet.FaltaEnvido)
            {
                pointsToAward = 1;
            }
            else
            {
                pointsToAward = (currentEnvidoPoints == 0) ? 1 : (currentEnvidoPoints - GetPointsForEnvidoBet(proposedEnvidoBet));

                if (pointsToAward == 0)
                {
                    pointsToAward = 1;
                }
            }

            NotifyResponse(NO_QUIERO_STATUS, caller.Username, TrucoBetValue.ToString());
            AwardEnvidoPoints(caller.Team, pointsToAward);
            ResetEnvidoState();
        }

        private void HandleEnvidoQuiero(PlayerInformation caller)
        {
            NotifyResponse(QUIERO_STATUS, caller.Username, TrucoBetValue.ToString());
            ResolveEnvido();
        }

        private void HandleEnvidoRaise(int playerID, string response)
        {
            ResetEnvidoState(false);
            CallEnvido(playerID, response);
        }

        private void AwardEnvidoPoints(string winningTeam, int points)
        {
            try
            {
                if (winningTeam == TEAM_1)
                {
                    Team1Score += points;
                }
                else
                {
                    Team2Score += points;
                }

                NotifyScoreUpdate();

                if (CheckMatchEnd())
                {
                    lock (matchLock)
                    {
                        matchEnded = true;
                    }

                    string winnerTeamString;
                    string matchWinnerName;
                    int winnerScore;
                    int loserScore;

                    if (Team1Score > Team2Score)
                    {
                        winnerTeamString = TEAM_1;
                        winnerScore = Team1Score;
                        loserScore = Team2Score;
                        matchWinnerName = Players.First(p => p.Team == TEAM_1).Username;
                    }
                    else
                    {
                        winnerTeamString = TEAM_2;
                        winnerScore = Team2Score;
                        loserScore = Team1Score;
                        matchWinnerName = Players.First(p => p.Team == TEAM_2).Username;
                    }

                    gameManager.SaveMatchResult(this.DbMatchId, winnerTeamString, winnerScore, loserScore);
                    NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(AwardEnvidoPoints));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AwardEnvidoPoints));
            }
        }

        private void AwardFlorPoints(string winningTeam, int points)
        {
            try
            {
                if (winningTeam == TEAM_1)
                {
                    Team1Score += points;
                }
                else
                {
                    Team2Score += points;
                }

                NotifyScoreUpdate();

                if (CheckMatchEnd())
                {
                    lock (matchLock)
                    {
                        matchEnded = true;
                    }
                    string winnerTeamString;
                    string matchWinnerName;
                    int winnerScore;
                    int loserScore;

                    if (Team1Score > Team2Score)
                    {
                        winnerTeamString = TEAM_1;
                        winnerScore = Team1Score;
                        loserScore = Team2Score;
                        matchWinnerName = Players.First(p => p.Team == TEAM_1).Username;
                    }
                    else
                    {
                        winnerTeamString = TEAM_2;
                        winnerScore = Team2Score;
                        loserScore = Team1Score;
                        matchWinnerName = Players.First(p => p.Team == TEAM_2).Username;
                    }

                    gameManager.SaveMatchResult(this.DbMatchId, winnerTeamString, winnerScore, loserScore);
                    NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(AwardFlorPoints));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AwardFlorPoints));
            }
        }

        private void EndHandWithPoints(string winningTeam, int pointsToAward)
        {
            lock (matchLock)
            {
                if (matchEnded)
                {
                    return;
                }
            }
            try
            {
                if (winningTeam == TEAM_1)
                {
                    Team1Score += pointsToAward;
                }
                else
                {
                    Team2Score += pointsToAward;
                }

                NotifyAll(callback => callback.NotifyScoreUpdate(Team1Score, Team2Score));

                if (CheckMatchEnd())
                {
                    HandleMatchWin();
                }
                else
                {
                    StartNewHand();
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(EndHandWithPoints));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(EndHandWithPoints));
            }
        }
        private PlayerInformation DetermineRoundWinner()
        {
            PlayerInformation roundWinner = null;
            TrucoCard highestCard = null;

            foreach (var entry in cardsOnTable)
            {
                var player = Players.First(p => p.PlayerID == entry.Key);
                var card = entry.Value;

                if (highestCard == null)
                {
                    highestCard = card;
                    roundWinner = player;
                }
                else
                {
                    int comparison = TrucoRules.CompareCards(card, highestCard);
                    if (comparison > 0)
                    {
                        highestCard = card;
                        roundWinner = player;
                    }
                    else if (comparison == 0)
                    {
                        roundWinner = null;
                    }
                }
            }
            return roundWinner;
        }

        private void ProcessRoundCompletion(PlayerInformation roundWinner)
        {
            string winnerTeam = (roundWinner == null) ? null : roundWinner.Team;

            if (currentRound < roundWinners.Length)
            {
                roundWinners[currentRound] = winnerTeam;
            }

            cardsOnTable.Clear();
            currentRound++;

            string winnerName = roundWinner?.Username ?? DRAW_STATUS;

            NotifyAll(callback => callback.NotifyRoundEnd(winnerName, Team1Score, Team2Score));
        }

        private void ProcessHandCompletion()
        {
            int team1Wins = roundWinners.Count(w => w == TEAM_1);
            int team2Wins = roundWinners.Count(w => w == TEAM_2);
            int pointsAwarded = GetPointsForBet(TrucoBetValue);

            if (team1Wins > team2Wins)
            {
                Team1Score += pointsAwarded;
            }
            else if (team2Wins > team1Wins)
            {
                Team2Score += pointsAwarded;
            }
            else
            {
                if (roundWinners[0] == TEAM_1)
                {
                    Team1Score += pointsAwarded;
                }
                else if (roundWinners[0] == TEAM_2)
                {
                    Team2Score += pointsAwarded;
                }
            }

            NotifyScoreUpdate();

            if (CheckMatchEnd())
            {
                HandleMatchWin();
            }
            else
            {
                StartNewHand();
            }
        }

        private bool CanInitiateTrucoCall()
        {
            if (waitingForEnvidoResponseId.HasValue)
            {
                return false;
            }

            if (waitingForResponseToId.HasValue)
            {
                return false;
            }

            if (CurrentState != GameState.Envido && CurrentState != GameState.Truco)
            {
                return false;
            }

            return true;
        }

        private bool IsInvalidTrucoBetSequence(TrucoBet newBet)
        {
            bool isInvalidJump = (newBet == TrucoBet.Truco && TrucoBetValue != TrucoBet.None) ||
                                 (newBet == TrucoBet.Retruco && TrucoBetValue != TrucoBet.Truco) ||
                                 (newBet == TrucoBet.ValeCuatro && TrucoBetValue != TrucoBet.Retruco);

            return isInvalidJump;
        }

        private void ApplyTrucoCallState(int playerID, TrucoBet newBet, int opponentID)
        {
            CurrentState = GameState.Truco;
            bettingPlayerId = playerID;
            waitingForResponseToId = opponentID;
            proposedBetValue = newBet;
        }

        private int DetermineContraFlorWinner(int id1, int id2)
        {
            int score1 = playerFlorScores[id1];
            int score2 = playerFlorScores[id2];

            if (score1 > score2)
            {
                return id1;
            }

            if (score2 > score1)
            {
                return id2;
            }

            return ResolveFlorTie(id1, id2);
        }

        private int ResolveFlorTie(int id1, int id2)
        {
            var manoPlayer = Players[handStartingPlayerIndex];

            if (Players.Count == 2)
            {
                return (manoPlayer.PlayerID == id1) ? id1 : id2;
            }

            var team1 = Players.First(p => p.PlayerID == id1).Team;

            if (manoPlayer.Team == team1)
            {
                return id1;
            }

            return id2;
        }

        private void HandleMatchWin()
        {
            lock (matchLock)
            {
                matchEnded = true;
            }
            string winnerTeamString;
            string matchWinnerName;
            int winnerScore;
            int loserScore;

            if (Team1Score > Team2Score)
            {
                winnerTeamString = TEAM_1;
                winnerScore = Team1Score;
                loserScore = Team2Score;
                matchWinnerName = Players.First(p => p.Team == TEAM_1).Username;
            }
            else
            {
                winnerTeamString = TEAM_2;
                winnerScore = Team2Score;
                loserScore = Team1Score;
                matchWinnerName = Players.First(p => p.Team == TEAM_2).Username;
            }

            gameManager.SaveMatchResult(this.DbMatchId, winnerTeamString, winnerScore, loserScore);
            NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
        }

        public void PlayerGoesToDeck(int playerID)
        {
            try
            {
                var player = Players.FirstOrDefault(p => p.PlayerID == playerID);

                if (player == null)
                {
                    return;
                }

                if (waitingForEnvidoResponseId.HasValue && waitingForEnvidoResponseId.Value == playerID)
                {
                    RespondToEnvido(playerID, NO_QUIERO_STATUS);
                }

                var opponent = GetOpponentToRespond(player);

                if (opponent == null)
                {
                    return;
                }

                int pointsToAward = GetPointsForBet(TrucoBetValue);
                NotifyResponse(AL_MAZO, player.Username, TrucoBetValue.ToString());
                EndHandWithPoints(opponent.Team, pointsToAward);
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(PlayerGoesToDeck));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(PlayerGoesToDeck));
            }
        }

        public void AbortMatch(string playerUsername)
        {
            lock (matchLock)
            {
                if (matchEnded)
                {
                    return;
                }
                matchEnded = true;
            }

            try
            {
                var leaver = Players.FirstOrDefault(p => p.Username == playerUsername);

                if (leaver == null)
                {
                    Console.WriteLine($"[ABORT] Player {playerUsername} not found in match");
                    return;
                }

                string loserTeam = leaver.Team;
                string winnerTeam = (loserTeam == TEAM_1) ? TEAM_2 : TEAM_1;

                int winnerScore = (winnerTeam == TEAM_1) ? Team1Score : Team2Score;
                int loserScore = (loserTeam == TEAM_1) ? Team1Score : Team2Score;

                var winnerPlayer = Players.FirstOrDefault(p => p.Team == winnerTeam);
                string matchWinnerName = winnerPlayer != null ? winnerPlayer.Username : "Oponent";

                Console.WriteLine($"[ABORT] Match {MatchCode} aborted by {playerUsername}. Winner: {matchWinnerName} (Guest: {leaver.PlayerID < 0})");

                gameManager.SaveMatchResult(this.DbMatchId, winnerTeam, winnerScore, loserScore);

                NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, nameof(AbortMatch));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AbortMatch));
            }
        }

        private void ResetFlorState(bool markAsPlayed = true)
        {
            FlorBetValue = FlorBet.None;
            currentFlorPoints = CURRENT_FLOR_SCORE;
            florBettorId = null;
            waitingForFlorResponseId = null;

            if (markAsPlayed)
            {
                florWasPlayed = true;
            }
        }

        private void ResetEnvidoState(bool markAsPlayed = true)
        {
            EnvidoBetValue = EnvidoBet.None;
            proposedEnvidoBet = EnvidoBet.None;
            currentEnvidoPoints = CURRENT_ENVIDO_POINT;
            envidoBettorId = null;
            waitingForEnvidoResponseId = null;

            if (markAsPlayed)
            {
                envidoWasPlayed = true;
            }
        }

        private void ResetBetState()
        {
            bettingPlayerId = null;
            waitingForResponseToId = null;
            proposedBetValue = TrucoBet.None;
        }

        private PlayerInformation GetOpponentToRespond(PlayerInformation caller)
        {
            try
            {
                int numPlayers = Players.Count;
                var currentTurnPlayer = GetCurrentTurnPlayer();

                if (currentTurnPlayer.Team != caller.Team)
                {
                    return currentTurnPlayer;
                }

                int nextOpponentIndex = (turnIndex + 1) % numPlayers;

                while (Players[nextOpponentIndex].Team == caller.Team)
                {
                    nextOpponentIndex = (nextOpponentIndex + 1) % numPlayers;

                    if (nextOpponentIndex == turnIndex)
                    {
                        return null;
                    }
                }

                return Players[nextOpponentIndex];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(GetOpponentToRespond));
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetOpponentToRespond));
                return null;
            }
        }

        private void NotifyAll(Action<ITrucoCallback> action)
        {
            foreach (var callback in PlayerCallbacks.Values)
            {
                try
                {
                    action(callback);
                }
                catch (CommunicationException ex)
                {
                    LogManager.LogWarn(ex.Message, nameof(NotifyAll));
                }
                catch (TimeoutException ex)
                {
                    LogManager.LogWarn(ex.Message, nameof(NotifyAll));
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, nameof(NotifyAll));
                }
            }
        }

        private void NotifyPlayer(int playerID, Action<ITrucoCallback> action)
        {
            if (PlayerCallbacks.TryGetValue(playerID, out var callback))
            {
                try
                {
                    action(callback);
                }
                catch (CommunicationException ex)
                {
                    LogManager.LogWarn(ex.Message, nameof(NotifyPlayer));
                }
                catch (TimeoutException ex)
                {
                    LogManager.LogWarn(ex.Message, nameof(NotifyPlayer));
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, nameof(NotifyPlayer));
                }
            }
        }

        private PlayerInformation GetCurrentTurnPlayer()
        {
            try
            {
                return Players[turnIndex];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(GetCurrentTurnPlayer));

                if (Players != null && Players.Count > 0)
                {
                    turnIndex = INDEX_VALUE;
                    return Players[0];
                }

                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetCurrentTurnPlayer));
                return null;
            }
        }

        private void NotifyTurnChange()
        {
            try
            {
                var nextPlayer = GetCurrentTurnPlayer();

                if (nextPlayer != null)
                {
                    NotifyAll(callback => callback.NotifyTurnChange(nextPlayer.Username));
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyTurnChange));
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyTurnChange));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyTurnChange));
            }
        }

        private void NotifyScoreUpdate()
        {
            try
            {
                NotifyAll(callback => callback.NotifyScoreUpdate(Team1Score, Team2Score));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyScoreUpdate));
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyScoreUpdate));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyScoreUpdate));
            }
        }

        private void NotifyResponse(string response, string callerName, string newBetState)
        {
            try
            {
                NotifyAll(callback => callback.NotifyResponse(callerName, response, newBetState));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyResponse));
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyResponse));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyResponse));
            }
        }

        private void NotifyTrucoCall(int callerId, string betName, int responderId)
        {
            try
            {
                var caller = Players.First(p => p.PlayerID == callerId);
                NotifyPlayer(responderId, callback => callback.NotifyTrucoCall(caller.Username, betName, true));

                foreach (var player in Players.Where(p => p.PlayerID != responderId))
                {
                    NotifyPlayer(player.PlayerID, callback => callback.NotifyTrucoCall(caller.Username, betName, false));
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(NotifyTrucoCall));
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyTrucoCall));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyTrucoCall));
            }
        }

        private void NotifyFlorCallHelper(int callerId, string betName, int responderId)
        {
            try
            {
                var caller = Players.First(p => p.PlayerID == callerId);
                NotifyPlayer(responderId, callback => callback.NotifyFlorCall(caller.Username, betName, true));

                foreach (var player in Players.Where(p => p.PlayerID != responderId))
                {
                    NotifyPlayer(player.PlayerID, callback => callback.NotifyFlorCall(caller.Username, betName, false));
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(NotifyFlorCallHelper));
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, nameof(NotifyFlorCallHelper));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyFlorCallHelper));
            }
        }

        private bool CheckHandWinner()
        {
            try
            {
                int team1Wins = roundWinners.Count(w => w == TEAM_1);
                int team2Wins = roundWinners.Count(w => w == TEAM_2);

                if (team1Wins >= 2 || team2Wins >= 2)
                {
                    return true;
                }

                if (currentRound >= MAX_ROUNDS)
                {
                    return true;
                }

                if (roundWinners[0] == null && currentRound == 2 && (team1Wins == 1 || team2Wins == 1))
                {
                    return true;
                }

                if (roundWinners[0] != null && roundWinners[1] == null && currentRound == 2)
                {
                    return true;
                }

                return false;
            }
            catch (IndexOutOfRangeException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(CheckHandWinner));
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CheckHandWinner));
                return false;
            }
        }

        private bool CheckMatchEnd()
        {
            return Team1Score >= MAX_SCORE || Team2Score >= MAX_SCORE;
        }

        private static int GetPointsForBet(TrucoBet bet)
        {
            try
            {
                switch (bet)
                {
                    case TrucoBet.Truco:
                        return 2;

                    case TrucoBet.Retruco:
                        return 3;

                    case TrucoBet.ValeCuatro:
                        return 4;

                    case TrucoBet.None:

                    default:
                        return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForBet));
                return 0;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForBet));
                return 0;
            }
        }

        private static int GetPointsForEnvidoBet(EnvidoBet bet)
        {
            try
            {
                switch (bet)
                {
                    case EnvidoBet.Envido:
                        return 2;

                    case EnvidoBet.RealEnvido:
                        return 3;
                                        
                    default:
                        return 0;
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForEnvidoBet));
                return 0;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForEnvidoBet));
                return 0;
            }
        }

        private int GetPointsForFaltaEnvido()
        {
            bool bothInMalas = Team1Score < 15 && Team2Score < 15;
            int targetScore = bothInMalas ? 15 : 30;

            int leadingScore = Math.Max(Team1Score, Team2Score);

            int pointsNeeded = targetScore - leadingScore;

            return pointsNeeded;
        }

        private static int GetPointsForFlorBet(FlorBet bet)
        {
            try
            {
                switch (bet)
                {
                    case FlorBet.Flor:
                        return 3;

                    case FlorBet.ContraFlor:
                        return 6;

                    default:
                        return 0;
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForFlorBet));
                return 0;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForFlorBet));
                return 0;
            }
        }
    }
}