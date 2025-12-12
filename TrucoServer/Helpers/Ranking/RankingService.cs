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
        private const string ERROR_CODE_DB_ERROR_RANKING = "ServerDBErrorRanking";
        private const string ERROR_CODE_GENERAL_ERROR = "ServerError";
        private const string ERROR_CODE_TIMEOUT_ERROR = "ServerTimeout";
        private const int TOP_RANKING = 10;


        public List<PlayerStatistics> GetGlobalRanking()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    return context.User
                        .OrderByDescending(u => u.wins)
                        .Take(TOP_RANKING)
                        .Select(u => new PlayerStatistics
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
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_RANKING, Lang.ExceptionTextDBErrorRanking);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_RANKING, Lang.ExceptionTextDBErrorRanking);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault(ERROR_CODE_TIMEOUT_ERROR, Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault(ERROR_CODE_GENERAL_ERROR, Lang.ExceptionTextErrorOcurred);
            }
        }
    }
}
