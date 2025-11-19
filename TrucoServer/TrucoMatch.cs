using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace TrucoServer
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
        ContraFlor,
        ContraFlorAlResto
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
        private const int INITIAL_SCORE = 0;
        private const int POINT = 1;
        private const int CURRENT_ROUND = 0;
        private const int CURRENT_ENVIDO_POINT = 0;
        private const int INDEX_VALUE = 0;
        private const int CURRENT_FLOR_SCORE = 0;
        private const int CURRENT_FLOR_POINT = 0;

        public string MatchCode { get; private set; }
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
        private int currentTrucoPoints;
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
        private FlorBet proposedFlorBet;
        private int currentFlorPoints;
        private int? florBettorId;
        private int? waitingForFlorResponseId;
        private bool florWasPlayed;
        private Dictionary<int, int> playerFlorScores;
        
        public TrucoMatch(
            string matchCode,
            List<PlayerInformation> players,
            Dictionary<int, ITrucoCallback> callbacks,
            ITrucoDeck deck,
            IGameManager gameManager)
        {
            try
            {
                this.MatchCode = matchCode;
                this.Players = players;
                this.PlayerCallbacks = callbacks;
                this.deck = deck;
                this.gameManager = gameManager;
                this.playerHands = new Dictionary<int, List<TrucoCard>>();
                this.playedCards = new Dictionary<int, List<TrucoCard>>();
                this.cardsOnTable = new Dictionary<int, TrucoCard>();
                this.roundWinners = new string[MAX_ROUNDS];
                this.Team1Score = INITIAL_SCORE;
                this.Team2Score = INITIAL_SCORE;
                this.CurrentState = GameState.Deal;
                this.TrucoBetValue = TrucoBet.None;
                this.currentTrucoPoints = POINT;
                this.handStartingPlayerIndex = 0; // Acá se debería modificar para q inicie uno al azar
                this.turnIndex = 0;

                foreach (var player in players)
                {
                    playerHands[player.PlayerID] = new List<TrucoCard>();
                    playedCards[player.PlayerID] = new List<TrucoCard>();
                }

                gameManager.SaveMatchToDatabase(matchCode, players);
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

        private static int GetPointsForBet(TrucoBet bet)
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
                    string loserTeamString;
                    string matchWinnerName;
                    int winnerScore;
                    int loserScore;

                    if (Team1Score > Team2Score)
                    {
                        loserTeamString = TEAM_2;
                        winnerScore = Team1Score;
                        loserScore = Team2Score;
                        matchWinnerName = Players.First(p => p.Team == TEAM_1).Username;
                    }
                    else
                    {
                        loserTeamString = TEAM_1;
                        winnerScore = Team2Score;
                        loserScore = Team1Score;
                        matchWinnerName = Players.First(p => p.Team == TEAM_2).Username;
                    }

                    gameManager.SaveMatchResult(MatchCode, loserTeamString, winnerScore, loserScore);
                    NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
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

        public void StartNewHand()
        {
            try
            {
                CurrentState = GameState.Deal;
                deck.Reset();
                deck.Shuffle();
                playerHands.Clear();
                cardsOnTable.Clear();

                roundWinners = new string[MAX_ROUNDS];
                currentRound = CURRENT_ROUND;

                TrucoBetValue = TrucoBet.None;
                currentTrucoPoints = POINT;
                bettingPlayerId = null;
                waitingForResponseToId = null;

                handStartingPlayerIndex = (handStartingPlayerIndex + 1) % Players.Count;
                turnIndex = handStartingPlayerIndex;

                foreach (var player in Players)
                {
                    var hand = deck.DealHand();
                    playerHands[player.PlayerID] = hand;
                    player.Hand = hand;
                    NotifyPlayer(player.PlayerID, callback => callback.ReceiveCards(hand.ToArray()));
                    gameManager.SaveDealtCards(MatchCode, player);
                }

                playerEnvidoScores = new Dictionary<int, int>();

                foreach (var player in Players)
                {
                    playerEnvidoScores[player.PlayerID] = TrucoRules.CalculateEnvidoScore(playerHands[player.PlayerID]);
                }

                EnvidoBetValue = EnvidoBet.None;
                proposedEnvidoBet = EnvidoBet.None;
                currentEnvidoPoints = CURRENT_ENVIDO_POINT;
                envidoBettorId = null;
                waitingForEnvidoResponseId = null;
                envidoWasPlayed = false;
                playerFlorScores = new Dictionary<int, int>();

                foreach (var player in Players)
                {
                    if (TrucoRules.HasFlor(playerHands[player.PlayerID]))
                    {
                        playerFlorScores[player.PlayerID] = TrucoRules.CalculateFlorScore(playerHands[player.PlayerID]);
                    }
                    else
                    {
                        playerFlorScores[player.PlayerID] = -1;
                    }
                }

                FlorBetValue = FlorBet.None;
                proposedFlorBet = FlorBet.None;
                currentFlorPoints = CURRENT_FLOR_POINT;
                florBettorId = null;
                waitingForFlorResponseId = null;
                florWasPlayed = false;

                CurrentState = GameState.Envido;
                NotifyTurnChange();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(StartNewHand));
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(PlayCard));
                return false;
            }
        }

        private void AdvanceTurn()
        {
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

                string winnerTeam = (roundWinner == null) ? null : roundWinner.Team;
                roundWinners[currentRound] = winnerTeam;
                cardsOnTable.Clear();
                currentRound++;
                string winnerName = roundWinner?.Username ?? DRAW_STATUS;
                gameManager.SaveRoundResult(MatchCode, winnerName);
                NotifyAll(callback => callback.NotifyRoundEnd(winnerName, Team1Score, Team2Score));

                if (CheckHandWinner())
                {
                    int teamAWins = roundWinners.Count(w => w == TEAM_1);
                    int teamBWins = roundWinners.Count(w => w == TEAM_2);
                    int pointsAwarded = GetPointsForBet(TrucoBetValue);

                    if (teamAWins > teamBWins)
                    {
                        Team1Score += pointsAwarded;
                    }
                    else if (teamBWins > teamAWins)
                    {
                        Team2Score += pointsAwarded;
                    }
                    else if (roundWinners[0] == TEAM_1)
                    {
                        Team1Score += pointsAwarded;
                    }
                    else if (roundWinners[0] == TEAM_2)
                    {
                        Team2Score += pointsAwarded;
                    }

                    NotifyAll(callback => callback.NotifyScoreUpdate(Team1Score, Team2Score));

                    if (CheckMatchEnd())
                    {
                        string loserTeamString;
                        string matchWinnerName;
                        int winnerScore;
                        int loserScore;

                        if (Team1Score > Team2Score)
                        {
                            loserTeamString = TEAM_2;
                            winnerScore = Team1Score;
                            loserScore = Team2Score;
                            matchWinnerName = Players.First(p => p.Team == TEAM_1).Username;
                        }
                        else
                        {
                            loserTeamString = TEAM_1;
                            winnerScore = Team2Score;
                            loserScore = Team1Score;
                            matchWinnerName = Players.First(p => p.Team == TEAM_2).Username;
                        }

                        gameManager.SaveMatchResult(MatchCode, loserTeamString, winnerScore, loserScore);
                        NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
                    }
                    else
                    {
                        StartNewHand();
                    }
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
            catch (NullReferenceException ex)
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

        public bool CallTruco(int playerID, string betType)
        {
            try
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

                var caller = Players.FirstOrDefault(p => p.PlayerID == playerID);
                
                if (caller == null)
                {
                    return false;
                }
                
                TrucoBet newBet;
                
                if (!Enum.TryParse(betType, out newBet))
                {
                    return false;
                }
                
                if ((newBet == TrucoBet.Truco && TrucoBetValue != TrucoBet.None) ||
                    (newBet == TrucoBet.Retruco && TrucoBetValue != TrucoBet.Truco) ||
                    (newBet == TrucoBet.ValeCuatro && TrucoBetValue != TrucoBet.Retruco))
                {
                    return false;
                }

                var opponent = GetOpponentToRespond(caller);
                
                if (opponent == null)
                {
                    return false;
                }
                
                CurrentState = GameState.Truco;
                bettingPlayerId = playerID;
                waitingForResponseToId = opponent.PlayerID;
                proposedBetValue = newBet;
                currentTrucoPoints = GetPointsForBet(newBet);
                NotifyTrucoCall(playerID, betType, opponent.PlayerID);
                
                return true;
            }
            catch (ArgumentException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(CallTruco));
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
                currentEnvidoPoints += GetPointsForEnvidoBet(newBet);
                NotifyAll(cb => cb.NotifyEnvidoCall(caller.Username, betType, currentEnvidoPoints, true));
                NotifyPlayer(opponent.PlayerID, cb => cb.NotifyEnvidoCall(caller.Username, betType, currentEnvidoPoints, true));

                return true;
            }
            catch (ArgumentException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(CallEnvido));
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
                
                var responder = Players.First(p => p.PlayerID == playerID);
                var caller = Players.First(p => p.PlayerID == envidoBettorId.Value);
                envidoWasPlayed = true;

                if (response == NO_QUIERO_STATUS)
                {
                    int pointsToAward = (currentEnvidoPoints == 0) ? 1 : (currentEnvidoPoints - GetPointsForEnvidoBet(proposedEnvidoBet));
                    
                    if (pointsToAward == 0)
                    {
                        pointsToAward = 1;
                    }
                    AwardEnvidoPoints(caller.Team, pointsToAward);
                    ResetEnvidoState();
                }
                else if (response == QUIERO_STATUS)
                {
                    ResolveEnvido();
                }
                else
                {
                    ResetEnvidoState(false);
                    CallEnvido(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(RespondToEnvido));
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
                
                NotifyAll(cb => cb.NotifyEnvidoResult(envidoWinner.Username, highestScore, currentEnvidoPoints));
                AwardEnvidoPoints(envidoWinner.Team, currentEnvidoPoints);
                ResetEnvidoState();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveEnvido));
            }
            catch (KeyNotFoundException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(ResolveEnvido));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveEnvido));
            }
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

        private int GetPointsForEnvidoBet(EnvidoBet bet)
        {
            try
            {
                switch (bet)
                {
                    case EnvidoBet.Envido:
                        return 2;

                    case EnvidoBet.RealEnvido:
                        return 3;
                    
                    case EnvidoBet.FaltaEnvido:
                        return MAX_SCORE - (Team1Score > Team2Score ? Team1Score : Team2Score);
                    
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForEnvidoBet));
                return 0;
            }
        }

        private bool CheckMatchEnd()
        {
            return Team1Score >= MAX_SCORE || Team2Score >= MAX_SCORE;
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyScoreUpdate));
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyTrucoCall));
            }
        }

        private void NotifyResponse(string response, string callerName, string newBetState)
        {
            try
            {
                NotifyAll(callback => callback.NotifyResponse(callerName, response, newBetState));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyResponse));
            }
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
                    return;
                }

                var winnerPlayer = Players.FirstOrDefault(p => p.Team != leaver.Team);

                if (winnerPlayer == null)
                {
                    return;
                }

                string winnerTeam = winnerPlayer.Team;
                string loserTeam = leaver.Team; 
                string matchWinnerName = winnerPlayer.Username;

                int winnerScore = MAX_SCORE;
                int loserScore = (loserTeam == TEAM_1) ? Team1Score : Team2Score;

                gameManager.SaveMatchResult(MatchCode, loserTeam, winnerScore, loserScore);
                NotifyAll(callback => callback.OnMatchEnded(MatchCode, matchWinnerName));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AbortMatch));
            }
        }

        private int GetPointsForFlorBet(FlorBet bet)
        {
            try
            {
                switch (bet)
                {
                    case FlorBet.Flor:
                        return 3;
                    
                    case FlorBet.ContraFlor:
                        return 6; 

                    case FlorBet.ContraFlorAlResto:
                        int leaderScore = (Team1Score > Team2Score) ? Team1Score : Team2Score;
                    
                        return MAX_SCORE - leaderScore;
                    
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPointsForFlorBet));
                return 0;
            }
        }

        public bool CallFlor(int playerID, string betType)
        {
            try
            {
                if (florWasPlayed)
                {
                    return false;
                }

                if (waitingForFlorResponseId.HasValue)
                {
                    return false;
                }

                if (CurrentState != GameState.Envido && CurrentState != GameState.Flor && CurrentState != GameState.Truco)
                {
                    if (CurrentState == GameState.Truco)
                    {
                        if (waitingForResponseToId.HasValue) return false;
                    }
                    else
                    {
                        return false;
                    }
                }

                var caller = Players.FirstOrDefault(p => p.PlayerID == playerID);
                
                if (caller == null)
                {
                    return false;
                }

                if (playerFlorScores[playerID] == -1)
                {
                    LogManager.LogWarn($"Jugador {caller.Username} intentó cantar Flor sin tenerla.", nameof(CallFlor));
                    return false;
                }

                FlorBet newBet;
                
                if (!Enum.TryParse(betType, out newBet))
                {
                    return false;
                }

                var opponent = GetOpponentToRespond(caller);
                
                if (opponent == null)
                {
                    return false;
                }

                if (CurrentState == GameState.Envido)
                {
                    ResetEnvidoState(false);
                }

                CurrentState = GameState.Flor;
                florBettorId = playerID;
                waitingForFlorResponseId = opponent.PlayerID;
                proposedFlorBet = newBet;
                currentFlorPoints = GetPointsForFlorBet(newBet);
                NotifyAll(cb => cb.NotifyFlorCall(caller.Username, betType, currentFlorPoints, true));

                return true;
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
                var caller = Players.First(p => p.PlayerID == florBettorId.Value);

                florWasPlayed = true;

                if (response == NO_QUIERO_STATUS)
                {
                    int pointsToAward = (FlorBetValue == FlorBet.None) ? 3 : GetPointsForFlorBet(FlorBetValue);
                    NotifyResponse(response, responder.Username, FlorBetValue.ToString());
                    AwardEnvidoPoints(caller.Team, pointsToAward);
                    ResetFlorState();
                    CurrentState = GameState.Truco;
                    NotifyTurnChange();
                }
                else if (response == QUIERO_STATUS)
                {
                    FlorBetValue = proposedFlorBet;

                    NotifyResponse(response, responder.Username, FlorBetValue.ToString());
                    ResolveFlor();

                    CurrentState = GameState.Truco;
                    NotifyTurnChange();
                }
                else
                {
                    if (playerFlorScores[playerID] == -1)
                    {
                        LogManager.LogWarn($"Jugador {responder.Username} intentó Contra-flor sin tener Flor.", nameof(RespondToFlor));
                        return;
                    }

                    ResetFlorState(false);
                    CallFlor(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(RespondToFlor));
            }
            catch (NullReferenceException ex)
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

                    if (score > -1)
                    {
                        if (score > highestScore)
                        {
                            highestScore = score;
                            florWinner = player;
                        }
                    }
                }

                if (florWinner != null)
                {
                    NotifyAll(cb => cb.NotifyEnvidoResult(florWinner.Username, highestScore, currentFlorPoints));
                    AwardEnvidoPoints(florWinner.Team, currentFlorPoints);
                }

                ResetFlorState();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveFlor));
            }
        }

        private void ResetFlorState(bool markAsPlayed = true)
        {
            FlorBetValue = FlorBet.None;
            proposedFlorBet = FlorBet.None;
            currentFlorPoints = CURRENT_FLOR_SCORE;
            florBettorId = null;
            waitingForFlorResponseId = null;
            
            if (markAsPlayed)
            {
                florWasPlayed = true;
            }
        }
    }
}