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
            catch (NotSupportedException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetGlobalRanking)} - LINQ Not Supported");
                return new List<PlayerStats>();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetGlobalRanking)} - SQL Error");
                return new List<PlayerStats>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetGlobalRanking)} - Invalid Operation (DataBase Context)");
                return new List<PlayerStats>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
        }
    }
}
