using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace TrucoServer
{
    public enum GameState { 
        Deal, 
        Envido, 
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

    public class TrucoMatch
    {
        private const int MAX_SCORE = 30;
        private const int CARDS_IN_HAND = 3;
        private const int MAX_ROUNDS = 3;
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";

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
        private Dictionary<int, int> playerEnvidoScores;

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
                this.Team1Score = 0;
                this.Team2Score = 0;
                this.CurrentState = GameState.Deal;
                this.TrucoBetValue = TrucoBet.None;
                this.currentTrucoPoints = 1;
                this.handStartingPlayerIndex = 0;
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
                currentRound = 0;

                TrucoBetValue = TrucoBet.None;
                currentTrucoPoints = 1;
                bettingPlayerId = null;
                waitingForResponseToId = null;

                handStartingPlayerIndex = (handStartingPlayerIndex + 1) % Players.Count;
                turnIndex = handStartingPlayerIndex;

                foreach (var player in Players)
                {
                    var hand = deck.DealHand();
                    playerHands[player.PlayerID] = hand;

                    player.Hand = hand;
                    gameManager.SaveDealtCards(MatchCode, player);
                    NotifyPlayer(player.PlayerID, callback => callback.ReceiveCards(hand.ToArray()));
                }
                playerEnvidoScores = new Dictionary<int, int>();
                foreach (var player in Players)
                {
                    playerEnvidoScores[player.PlayerID] = TrucoRules.CalculateEnvidoScore(playerHands[player.PlayerID]);
                }
                EnvidoBetValue = EnvidoBet.None;
                proposedEnvidoBet = EnvidoBet.None;
                currentEnvidoPoints = 0;
                envidoBettorId = null;
                waitingForEnvidoResponseId = null;
                envidoWasPlayed = false;
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
                if (waitingForEnvidoResponseId.HasValue)
                {
                    return false;
                }
                var player = GetCurrentTurnPlayer();
                if (player.PlayerID != playerID || (CurrentState != GameState.Truco && CurrentState != GameState.Envido))
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
                string winnerName = roundWinner?.Username ?? "Empate";
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
                if (response == "NoQuiero")
                {
                    int pointsToAward = GetPointsForBet(TrucoBetValue);
                    NotifyResponse(response, responder.Username, TrucoBetValue.ToString());
                    EndHandWithPoints(caller.Team, pointsToAward);
                }
                else if (response == "Quiero")
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

                if (response == "NoQuiero")
                {
                    int pointsToAward = (currentEnvidoPoints == 0) ? 1 : (currentEnvidoPoints - GetPointsForEnvidoBet(proposedEnvidoBet));
                    if (pointsToAward == 0)
                    {
                        pointsToAward = 1;
                    }
                    AwardEnvidoPoints(caller.Team, pointsToAward);
                    ResetEnvidoState();
                }
                else if (response == "Quiero")
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
            currentEnvidoPoints = 0;
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
                    turnIndex = 0;
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
    }
}