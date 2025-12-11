using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using TrucoServer.Data.DTOs;
using TrucoServer.Langs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Ranking
{
    public class MatchHistoryService : IMatchHistoryService
    {
        private const string ERROR_CODE_DB_ERROR_HISTORY = "ServerDBErrorHistory";
        private const string ERROR_CODE_GENERAL_ERROR = "ServerError";
        private const string ERROR_CODE_TIMEOUT_ERROR = "ServerTimeout";

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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetLastMatches));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_HISTORY, Lang.ExceptionTextDBErrorHistory);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetLastMatches));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_HISTORY, Lang.ExceptionTextDBErrorHistory);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetLastMatches));
                throw FaultFactory.CreateFault(ERROR_CODE_TIMEOUT_ERROR, Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLastMatches));
                throw FaultFactory.CreateFault(ERROR_CODE_GENERAL_ERROR, Lang.ExceptionTextErrorOcurred);
            }
        }
    }
}
