using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Ranking
{
    public interface IRankingService
    {
        List<PlayerStats> GetGlobalRanking();
    }
}
