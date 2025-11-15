using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private const string TEAM_A = "Team A";
        private const string TEAM_B = "Team B";

        public string matchCode { get; private set; }
        public List<PlayerInformation> players { get; private set; }
        public Dictionary<int, ITrucoCallback> playerCallbacks { get; private set; }
        public int team1Score { get; private set; }
        public int team2Score { get; private set; }
        public GameState currentState { get; private set; }

        private readonly ITrucoDeck deck;
        private readonly IGameManager gameManager;
        private Dictionary<int, List<TrucoCard>> playerHands;
        private Dictionary<int, List<TrucoCard>> playedCards;
        private Dictionary<int, TrucoCard> cardsOnTable;

        private string[] roundWinners;
        private int currentRound;
        private int handStartingPlayerIndex;
        private int turnIndex;

        public TrucoBet trucoBetValue { get; private set; }
        private int currentTrucoPoints;
        private int? bettingPlayerId;
        private int? waitingForResponseToId;
        private TrucoBet proposedBetValue;
        public EnvidoBet envidoBetValue { get; private set; }
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
            this.matchCode = matchCode;
            this.players = players;
            this.playerCallbacks = callbacks;
            this.deck = deck;
            this.gameManager = gameManager;
            this.playerHands = new Dictionary<int, List<TrucoCard>>();
            this.playedCards = new Dictionary<int, List<TrucoCard>>();
            this.cardsOnTable = new Dictionary<int, TrucoCard>();
            this.roundWinners = new string[MAX_ROUNDS];
            this.team1Score = 0;
            this.team2Score = 0;
            this.currentState = GameState.Deal;
            this.trucoBetValue = TrucoBet.None;
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

        private int GetPointsForBet(TrucoBet bet)
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
            int numPlayers = players.Count;
            var currentTurnPlayer = GetCurrentTurnPlayer();

            if (currentTurnPlayer.Team != caller.Team)
            {
                return currentTurnPlayer;
            }

            int nextOpponentIndex = (turnIndex + 1) % numPlayers;
            while (players[nextOpponentIndex].Team == caller.Team)
            {
                nextOpponentIndex = (nextOpponentIndex + 1) % numPlayers;

                if (nextOpponentIndex == turnIndex) return null;
            }
            return players[nextOpponentIndex];
        }

        private void EndHandWithPoints(string winningTeam, int pointsToAward)
        {
            if (winningTeam == TEAM_A)
            {
                team1Score += pointsToAward;
            }
            else
            {
                team2Score += pointsToAward;
            }

            NotifyAll(callback => callback.NotifyScoreUpdate(team1Score, team2Score));

            if (CheckMatchEnd())
            {
                string loserTeamString;
                string matchWinnerName;
                int winnerScore;
                int loserScore;

                if (team1Score > team2Score)
                {
                    loserTeamString = TEAM_B;
                    winnerScore = team1Score;
                    loserScore = team2Score;
                    matchWinnerName = players.First(p => p.Team == TEAM_A).Username;
                }
                else
                {
                    loserTeamString = TEAM_A;
                    winnerScore = team2Score;
                    loserScore = team1Score;
                    matchWinnerName = players.First(p => p.Team == TEAM_B).Username;
                }
                gameManager.SaveMatchResult(matchCode, loserTeamString, winnerScore, loserScore);
                NotifyAll(callback => callback.OnMatchEnded(matchCode, matchWinnerName));
            }
            else
            {
                StartNewHand();
            }
        }

        public void StartNewHand()
        {
            currentState = GameState.Deal;
            deck.Reset();
            deck.Shuffle();
            playerHands.Clear();
            cardsOnTable.Clear();

            roundWinners = new string[MAX_ROUNDS];
            currentRound = 0;

            trucoBetValue = TrucoBet.None;
            currentTrucoPoints = 1;
            bettingPlayerId = null;
            waitingForResponseToId = null;

            handStartingPlayerIndex = (handStartingPlayerIndex + 1) % players.Count;
            turnIndex = handStartingPlayerIndex;

            foreach (var player in players)
            {
                var hand = deck.DealHand();
                playerHands[player.PlayerID] = hand;

                player.Hand = hand;
                gameManager.SaveDealtCards(matchCode, player);
                NotifyPlayer(player.PlayerID, callback => callback.ReceiveCards(hand.ToArray()));
            }
            playerEnvidoScores = new Dictionary<int, int>();
            foreach (var player in players)
            {
                playerEnvidoScores[player.PlayerID] = TrucoRules.CalculateEnvidoScore(playerHands[player.PlayerID]);
            }
            envidoBetValue = EnvidoBet.None;
            proposedEnvidoBet = EnvidoBet.None;
            currentEnvidoPoints = 0;
            envidoBettorId = null;
            waitingForEnvidoResponseId = null;
            envidoWasPlayed = false;
            currentState = GameState.Envido;
            NotifyTurnChange();
        }

        public bool PlayCard(int playerID, string cardFileName)
        {
            if (waitingForEnvidoResponseId.HasValue)
            {
                return false;
            }
            var player = GetCurrentTurnPlayer();
            if (player.PlayerID != playerID || (currentState != GameState.Truco && currentState != GameState.Envido))
            {
                return false;
            }

            if (!playerHands.ContainsKey(playerID)) return false;

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

        private void AdvanceTurn()
        {
            turnIndex = (turnIndex + 1) % players.Count;

            if (cardsOnTable.Count == players.Count)
            {
                ResolveCurrentRound();
            }
            else
            {
                NotifyTurnChange();
            }
        }

        private void ResolveCurrentRound()
        {
            PlayerInformation roundWinner = null;
            TrucoCard highestCard = null;

            foreach (var entry in cardsOnTable)
            {
                var player = players.First(p => p.PlayerID == entry.Key);
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
            gameManager.SaveRoundResult(matchCode, winnerName);
            NotifyAll(callback => callback.NotifyRoundEnd(winnerName, team1Score, team2Score));
            if (CheckHandWinner())
            {
                int teamAWins = roundWinners.Count(w => w == TEAM_A);
                int teamBWins = roundWinners.Count(w => w == TEAM_B);
                int pointsAwarded = GetPointsForBet(trucoBetValue);

                if (teamAWins > teamBWins)
                {
                    team1Score += pointsAwarded;
                }
                else if (teamBWins > teamAWins)
                {
                    team2Score += pointsAwarded;
                }
                else if (roundWinners[0] == TEAM_A)
                {
                    team1Score += pointsAwarded;
                }
                else if (roundWinners[0] == TEAM_B)
                {
                    team2Score += pointsAwarded;
                }
                NotifyAll(callback => callback.NotifyScoreUpdate(team1Score, team2Score));

                if (CheckMatchEnd())
                {
                    string loserTeamString;
                    string matchWinnerName;
                    int winnerScore;
                    int loserScore;

                    if (team1Score > team2Score)
                    {
                        loserTeamString = TEAM_B;
                        winnerScore = team1Score;
                        loserScore = team2Score;
                        matchWinnerName = players.First(p => p.Team == TEAM_A).Username;
                    }
                    else
                    {
                        loserTeamString = TEAM_A;
                        winnerScore = team2Score;
                        loserScore = team1Score;
                        matchWinnerName = players.First(p => p.Team == TEAM_B).Username;
                    }
                    gameManager.SaveMatchResult(matchCode, loserTeamString, winnerScore, loserScore);
                    NotifyAll(callback => callback.OnMatchEnded(matchCode, matchWinnerName));
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

        private bool CheckHandWinner()
        {
            int team1Wins = roundWinners.Count(w => w == TEAM_A);
            int team2Wins = roundWinners.Count(w => w == TEAM_B);

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

        public bool CallTruco(int playerID, string betType)
        {
            if (waitingForEnvidoResponseId.HasValue) 
            {
                return false; 
            }
            if (waitingForResponseToId.HasValue)
            {
                return false;
            }
            if (currentState != GameState.Envido && currentState != GameState.Truco) 
            {
                return false;
            }
            var caller = players.FirstOrDefault(p => p.PlayerID == playerID);
            if (caller == null)
            {
                return false;
            }
            TrucoBet newBet;
            if (!Enum.TryParse(betType, out newBet))
            {
                return false;
            }
            if ((newBet == TrucoBet.Truco && trucoBetValue != TrucoBet.None) ||
                (newBet == TrucoBet.Retruco && trucoBetValue != TrucoBet.Truco) ||
                (newBet == TrucoBet.ValeCuatro && trucoBetValue != TrucoBet.Retruco))
            {
                return false;
            }

            var opponent = GetOpponentToRespond(caller);
            if (opponent == null)
            {
                return false;
            }
            currentState = GameState.Truco; 
            bettingPlayerId = playerID;
            waitingForResponseToId = opponent.PlayerID;
            proposedBetValue = newBet; 
            currentTrucoPoints = GetPointsForBet(newBet); 
            NotifyTrucoCall(playerID, betType, opponent.PlayerID);
            return true;
        }

        public void RespondToCall(int playerID, string response)
        {
            if (!waitingForResponseToId.HasValue || waitingForResponseToId.Value != playerID)
            {
                return;
            }
            var responder = players.First(p => p.PlayerID == playerID);
            var caller = players.First(p => p.PlayerID == bettingPlayerId.Value);
            if (response == "NoQuiero")
            {
                int pointsToAward = GetPointsForBet(trucoBetValue);
                NotifyResponse(response, responder.Username);
                EndHandWithPoints(caller.Team, pointsToAward);
            }
            else if (response == "Quiero")
            {
                trucoBetValue = proposedBetValue;
                NotifyResponse(response, responder.Username);
                ResetBetState();
                NotifyTurnChange();
            }
            else
            {
                ResetBetState();
                CallTruco(playerID, response); 
            }
        }

        public bool CallEnvido(int playerID, string betType)
        {
            if (envidoWasPlayed)
            {
                return false;
            }
            if (waitingForEnvidoResponseId.HasValue)
            {
                return false;
            }
            if (currentState != GameState.Envido)
            {
                return false;
            }
            var caller = players.FirstOrDefault(p => p.PlayerID == playerID);
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

        public void RespondToEnvido(int playerID, string response)
        {
            if (!waitingForEnvidoResponseId.HasValue || waitingForEnvidoResponseId.Value != playerID)
            {
                return;
            }
            var responder = players.First(p => p.PlayerID == playerID);
            var caller = players.First(p => p.PlayerID == envidoBettorId.Value);
            envidoWasPlayed = true; 

            if (response == "NoQuiero")
            {
                int pointsToAward = (currentEnvidoPoints == 0) ? 1 : (currentEnvidoPoints - GetPointsForEnvidoBet(proposedEnvidoBet));
                if (pointsToAward == 0) pointsToAward = 1; 
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

        private void ResolveEnvido()
        {
            PlayerInformation envidoWinner = null;
            int highestScore = -1;
            for (int i = 0; i < players.Count; i++)
            {
                int playerIndex = (handStartingPlayerIndex + i) % players.Count;
                var player = players[playerIndex];
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

        private void AwardEnvidoPoints(string winningTeam, int points)
        {
            if (winningTeam == TEAM_A)
            {
                team1Score += points;
            }
            else
            {
                team2Score += points;
            }
            NotifyScoreUpdate();
        }

        private void ResetEnvidoState(bool markAsPlayed = true)
        {
            envidoBetValue = EnvidoBet.None;
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
            switch (bet)
            {
                case EnvidoBet.Envido:
                    return 2;
                case EnvidoBet.RealEnvido:
                    return 3;
                case EnvidoBet.FaltaEnvido:
                    // TODO: Lógica de FaltaEnvido (puntos restantes hasta MAX_SCORE)
                    return MAX_SCORE - (team1Score > team2Score ? team1Score : team2Score);
                default:
                    return 0;
            }
        }

        private bool CheckMatchEnd()
        {
            return team1Score >= MAX_SCORE || team2Score >= MAX_SCORE;
        }

        private PlayerInformation GetCurrentTurnPlayer()
        {
            return players[turnIndex];
        }

        private void NotifyPlayer(int playerID, Action<ITrucoCallback> action)
        {
            if (playerCallbacks.TryGetValue(playerID, out var callback))
            {
                try
                {
                    action(callback);
                }
                catch (Exception) 
                { 

                }
            }
        }

        private void NotifyAll(Action<ITrucoCallback> action)
        {
            foreach (var callback in playerCallbacks.Values)
            {
                try
                {
                    action(callback);
                }
                catch (Exception) 
                { 
                
                }
            }
        }

        private void NotifyTurnChange()
        {
            var nextPlayer = GetCurrentTurnPlayer();
            NotifyAll(callback => callback.NotifyTurnChange(nextPlayer.Username));
        }

        private void NotifyScoreUpdate()
        {
            NotifyAll(callback => callback.NotifyScoreUpdate(team1Score, team2Score));
        }

        private void NotifyTrucoCall(int callerId, string betName, int responderId)
        {
            var caller = players.First(p => p.PlayerID == callerId);
            NotifyPlayer(responderId, callback => callback.NotifyTrucoCall(caller.Username, betName, true));

            foreach (var player in players.Where(p => p.PlayerID != responderId))
            {
                NotifyPlayer(player.PlayerID, callback => callback.NotifyTrucoCall(caller.Username, betName, false));
            }
        }

        private void NotifyResponse(string response, string callerName)
        {
            NotifyAll(callback => callback.NotifyResponse(callerName, response));
        }
    }
}
