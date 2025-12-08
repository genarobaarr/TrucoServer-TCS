using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
        }
    }
}
