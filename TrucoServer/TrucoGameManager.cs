using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;

namespace TrucoServer
{
    public class TrucoGameManager : IGameManager
    {
        private const string ROUND_INPROGRESS = "InProgress";
        private const string ROUND_FINISHED = "Finished";
        private const string ROUND_PLAYING = "Playing";

        private const int INITIAL_SCORE = 0;
        private const int VERSION_1V1 = 1;
        private const int VERSION_2V2 = 2;
        private const int HASH_MODULUS = 999999;
        private const int HASH_MULTIPLIER = 31;
        private const int HASH_SEED = 17;

        public int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformation> players)
        {
            try
            {
                using (var context = GetContext())
                {
                    var existingMatch = GetExistingInProgressMatch(context, lobbyId);

                    if (existingMatch != null)
                    {
                        return existingMatch.matchID;
                    }
                    
                    var match = CreateAndSaveMatch(context, lobbyId, players?.Count ?? 0);
                    
                    if (players != null && players.Any())
                    {
                        AddPlayersToMatch(context, match.matchID, players);
                    }
                    
                    CreateInitialRound(context, match.matchID);

                    return match.matchID;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
        }

        public void SaveDealtCards(string matchCode, PlayerInformation player)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = GetMatchByCode(context, matchCode);

                    if (match == null)
                    {
                        return;
                    }

                    var round = context.Round.FirstOrDefault(r => r.matchID == match.matchID && r.isActive == true);

                    if (round == null)
                    {
                        return;
                    }

                    var user = context.User.FirstOrDefault(u => u.username == player.Username);

                    if (user == null)
                    {
                        return;
                    }

                    foreach (var card in player.Hand)
                    {
                        var cardEntity = context.Card.FirstOrDefault(c =>
                            c.suit == card.CardSuit.ToString() &&
                            c.rank == card.CardRank.ToString());

                        if (cardEntity != null)
                        {
                            context.DealtCard.Add(new DealtCard
                            {
                                roundID = round.roundID,
                                playerID = user.userID,
                                cardID = cardEntity.cardID
                            });
                        }
                    }

                    context.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveDealtCards));
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveDealtCards));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveDealtCards));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveDealtCards));
            }
        }

        public void SaveRoundResult(string matchCode, string winner)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = GetMatchByCode(context, matchCode);

                    if (match == null)
                    {
                        return;
                    }

                    var round = context.Round.FirstOrDefault(r => r.matchID == match.matchID && r.isActive == true);

                    if (round == null)
                    {
                        return;
                    }

                    var winnerUsername = context.User.FirstOrDefault(u => u.username == winner);

                    round.status = ROUND_FINISHED;
                    round.isActive = false;
                    round.winnerID = winnerUsername?.userID;

                    context.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
        }

        public void SaveMatchResult(int matchId, string winnerTeam, int winnerScore, int loserScore)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = context.Match.Find(matchId);

                    if (match == null)
                    {
                        LogManager.LogError(new Exception($"[CREATE MATCH] Match ID {matchId} not found in DB"), nameof(SaveMatchResult));
                        return;
                    }

                    match.status = "Finished";
                    match.endedAt = DateTime.Now;

                    var dbPlayers = context.MatchPlayer.Where(mp => mp.matchID == matchId).ToList();

                    foreach (var mp in dbPlayers)
                    {
                        UpdatePlayerAndUserStats(context, mp, winnerTeam, winnerScore, loserScore);
                    }

                    context.SaveChanges();
                    Console.WriteLine($"[GAME] Match {matchId} completed successfully. Winner: {winnerTeam}.");
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
        }

        private static baseDatosTrucoEntities GetContext()
        {
            return new baseDatosTrucoEntities();
        }

        private static void UpdatePlayerAndUserStats(baseDatosTrucoEntities context, MatchPlayer mp, string winnerTeam, int winnerScore, int loserScore)
        {
            bool isWinnerTeam = string.Equals(mp.team.Trim(), winnerTeam.Trim(), StringComparison.OrdinalIgnoreCase);

            mp.isWinner = isWinnerTeam;
            mp.score = isWinnerTeam ? winnerScore : loserScore;

            var userStats = context.User.FirstOrDefault(u => u.userID == mp.userID);
            if (userStats != null)
            {
                if (isWinnerTeam)
                {
                    userStats.wins++;
                }
                else
                {
                    userStats.losses++;
                }
            }
        }

        private Match CreateAndSaveMatch(baseDatosTrucoEntities context, int lobbyId, int playerCount)
        {
            int versionId = (playerCount == 4) ? VERSION_2V2 : VERSION_1V1;

            var match = new Match
            {
                lobbyID = lobbyId,
                versionID = versionId,
                status = ROUND_INPROGRESS,
                startedAt = DateTime.Now
            };

            context.Match.Add(match);
            context.SaveChanges();
            return match;
        }

        private void AddPlayersToMatch(baseDatosTrucoEntities context, int matchId, List<PlayerInformation> players)
        {
            foreach (var p in players)
            {
                var user = context.User.FirstOrDefault(u => u.username == p.Username);

                if (user == null) continue;

                context.MatchPlayer.Add(new MatchPlayer
                {
                    matchID = matchId,
                    userID = user.userID,
                    team = p.Team,
                    score = INITIAL_SCORE,
                    isWinner = false
                });
            }
            context.SaveChanges();
        }

        private void CreateInitialRound(baseDatosTrucoEntities context, int matchId)
        {
            var round = new Round
            {
                matchID = matchId,
                number = 1,
                status = ROUND_PLAYING,
                isActive = true
            };

            context.Round.Add(round);
            context.SaveChanges();
        }

        private static int GenerateNumericCodeFromString(string code)
        {
            unchecked
            {
                int hash = HASH_SEED;
                
                foreach (char c in code)
                {
                    hash = hash * HASH_MULTIPLIER + c;
                }
                return Math.Abs(hash % HASH_MODULUS);
            }
        }

        private static Match GetMatchByCode(baseDatosTrucoEntities context, string matchCode)
        {
            try {
                int numericCode = GenerateNumericCodeFromString(matchCode);

                var invitation = context.Invitation
                    .Where(i => i.code == numericCode)
                    .OrderByDescending(i => i.expiresAt)
                    .FirstOrDefault();

                if (invitation == null)
                {
                    return null;
                }

                var lobby = context.Lobby
                    .Where(l => l.ownerID == invitation.senderID)
                    .OrderByDescending(l => l.createdAt)
                    .FirstOrDefault();

                if (lobby == null)
                {
                    return null;
                }

                return context.Match
                    .Where(m => m.lobbyID == lobby.lobbyID && m.status == ROUND_INPROGRESS)
                    .OrderByDescending(m => m.startedAt)
                    .FirstOrDefault();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(GetMatchByCode));
                return new Match();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(GetMatchByCode));
                return new Match();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetMatchByCode));
                return new Match();
            }
        }

        private static Match GetExistingInProgressMatch(baseDatosTrucoEntities context, int lobbyId)
        {
            return context.Match
                .FirstOrDefault(m => m.lobbyID == lobbyId && m.status == ROUND_INPROGRESS);
        }
    }
}