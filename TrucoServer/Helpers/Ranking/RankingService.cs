using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Ranking
{
    public class RankingService : IRankingService
    {
        public List<PlayerStats> GetGlobalRanking()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    return context.User
                        .OrderByDescending(u => u.wins)
                        .Take(10)
                        .Select(u => new PlayerStats
                        {
                            PlayerName = u.username,
                            Wins = u.wins,
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                LogManager.LogError(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
        }
    }
}
