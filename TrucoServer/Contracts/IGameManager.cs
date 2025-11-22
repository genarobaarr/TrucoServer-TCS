using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    public interface IGameManager
    {
        int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformation> players);
        void SaveMatchResult(int matchId, string winnerTeam, int winnerScore, int loserScore);
    }
}
