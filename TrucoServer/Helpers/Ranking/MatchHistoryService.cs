using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Ranking
{
    public class MatchHistoryService : IMatchHistoryService
    {
        public List<MatchScore> GetLastMatches(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return new List<MatchScore>();
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        LogManager.LogWarn($"[WARN] History attempt for user not found: {username}", nameof(GetLastMatches));
                        return new List<MatchScore>();
                    }

                    return context.MatchPlayer
                        .Where(mp => mp.userID == user.userID)
                        .Select(mp => new { MatchPlayer = mp, Match = mp.Match })
                        .Where(join => join.Match.status == "Finished" && join.Match.endedAt.HasValue)
                        .OrderByDescending(join => join.Match.endedAt)
                        .Take(5)
                        .Select(join => new MatchScore
                        {
                            MatchID = join.Match.matchID.ToString(),
                            EndedAt = join.Match.endedAt.Value,
                            IsWin = join.MatchPlayer.isWinner,
                            FinalScore = join.MatchPlayer.score
                        })
                        .ToList();
                }
            }
            catch (NotSupportedException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLastMatches)} - LINQ Not Supported");
                return new List<MatchScore>();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLastMatches)} - SQL Error");
                return new List<MatchScore>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLastMatches)} - Invalid Operation (DataBase Context)");
                return new List<MatchScore>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetLastMatches));
                return new List<MatchScore>();
            }
        }
    }
}
