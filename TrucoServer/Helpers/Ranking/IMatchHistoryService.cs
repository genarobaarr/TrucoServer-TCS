using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Ranking
{
    public interface IMatchHistoryService
    {
        List<MatchScore> GetLastMatches(string username);
    }
}
