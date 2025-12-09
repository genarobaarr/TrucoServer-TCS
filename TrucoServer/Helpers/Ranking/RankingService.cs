using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Data.DTOs;
using TrucoServer.Langs;
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
                throw FaultFactory.CreateFault("ServerDBErrorRanking", Lang.ExceptionTextDBErrorRanking);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault("ServerDBErrorRanking", Lang.ExceptionTextDBErrorRanking);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
        }
    }
}
