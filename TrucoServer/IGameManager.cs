using System.Collections.Generic;
namespace TrucoServer
{
    public interface IGameManager
    {
        int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformation> players);
        void SaveMatchResult(int matchId, string winnerTeam, int winnerScore, int loserScore);
    }
}
