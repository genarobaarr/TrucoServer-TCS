using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public class TrucoGameManager : IGameManager
    {
        private const string STATUS_INPROGRESS = "InProgress";
        private const string STATUS_FINISHED = "Finished";
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";

        private const int INITIAL_SCORE = 0;
        private const int VERSION_1V1 = 1;
        private const int VERSION_2V2 = 2;

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
                        var registeredPlayers = players.Where(p => p.PlayerID > 0).ToList();

                        if (registeredPlayers.Any())
                        {
                            AddPlayersToMatch(context, match.matchID, registeredPlayers);
                        }
                    }

                    return match.matchID;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchToDatabase));
                return -1;
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

                    match.status = STATUS_FINISHED;
                    match.endedAt = DateTime.Now;

                    var dbPlayers = context.MatchPlayer.Where(mp => mp.matchID == matchId).ToList();

                    foreach (var mp in dbPlayers)
                    {
                        UpdatePlayerAndUserStats(context, mp, winnerTeam, winnerScore, loserScore);
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchResult));
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
                status = STATUS_INPROGRESS,
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
                if (p.PlayerID <= 0)
                {
                    continue;
                }

                var user = context.User.FirstOrDefault(u => u.userID == p.PlayerID);

                if (user == null)
                {
                    continue;
                }

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

        private static Match GetExistingInProgressMatch(baseDatosTrucoEntities context, int lobbyId)
        {
            return context.Match
                .FirstOrDefault(m => m.lobbyID == lobbyId && m.status == STATUS_INPROGRESS);
        }
    }
}