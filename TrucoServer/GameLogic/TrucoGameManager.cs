using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Ranking;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public class TrucoGameManager : IGameManager
    {
        private const string STATUS_INPROGRESS = "InProgress";
        private const string STATUS_FINISHED = "Finished";

        private const int INITIAL_SCORE = 0;
        private const int VERSION_1V1 = 1;
        private const int VERSION_2V2 = 2;

        private readonly IUserStatsService userStatsService;
        private readonly baseDatosTrucoEntities context;

        public TrucoGameManager(baseDatosTrucoEntities context, IUserStatsService userStatsService)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.userStatsService = userStatsService ?? throw new ArgumentNullException(nameof(userStatsService));
        }

        public int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformationWithConstructor> players)
        {
            try
            {
                var existingMatch = GetExistingInProgressMatch(lobbyId);

                if (existingMatch != null)
                {
                    return existingMatch.matchID;
                }
                    
                var match = CreateAndSaveMatch(lobbyId, players?.Count ?? 0);
                    
                if (players != null && players.Any())
                {
                    var registeredPlayers = players.Where(p => p.PlayerID > 0).ToList();

                    if (registeredPlayers.Any())
                    {
                        AddPlayersToMatch(match.matchID, registeredPlayers);
                    }
                }

                return match.matchID;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
        }

        public void SaveMatchResult(int matchId, MatchOutcome outcome)
        {
            try
            {
                if (outcome == null)
                { 
                    throw new ArgumentNullException(nameof(outcome));
                }

                var match = context.Match.Find(matchId);
               
                if (match == null) 
                {
                    return;
                }

                match.status = STATUS_FINISHED;
                match.endedAt = DateTime.Now;

                var dbPlayers = context.MatchPlayer.Where(mp => mp.matchID == matchId).ToList();

                foreach (var mp in dbPlayers)
                {
                    UpdateMatchPlayerResult(mp, outcome);

                    userStatsService.UpdateUserStats(mp.userID, mp.isWinner);
                    userStatsService.UpdateUserStats(mp.userID, mp.isWinner);
                }

                context.SaveChanges();
                
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchResult));
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchResult));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchResult));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchResult));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SaveMatchResult));
            }
        }

        private static void UpdateMatchPlayerResult(MatchPlayer mp, MatchOutcome outcome)
        {
            bool isWinnerTeam = string.Equals(mp.team.Trim(), outcome.WinnerTeam.Trim(), StringComparison.OrdinalIgnoreCase);

            mp.isWinner = isWinnerTeam;
            mp.score = isWinnerTeam ? outcome.WinnerScore : outcome.LoserScore;
        }

        private Match CreateAndSaveMatch(int lobbyId, int playerCount)
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

        private void AddPlayersToMatch(int matchId, List<PlayerInformationWithConstructor> players)
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

        private Match GetExistingInProgressMatch(int lobbyId)
        {
            return context.Match
                .FirstOrDefault(m => m.lobbyID == lobbyId && m.status == STATUS_INPROGRESS);
        }
    }
}