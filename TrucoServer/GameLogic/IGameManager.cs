using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.GameLogic
{
    public interface IGameManager
    {
        int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformation> players);
        void SaveMatchResult(int matchId, MatchOutcome outcome);
    }
}
