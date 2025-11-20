using System.Collections.Generic;
namespace TrucoServer
{
    public interface IGameManager
    {
        int SaveMatchToDatabase(string matchCode, int lobbyId, List<PlayerInformation> players);
        void SaveDealtCards(string matchCode, PlayerInformation player);
        void SaveRoundResult(string matchCode, string winner);
        void SaveMatchResult(int matchId, string winnerTeam, int winnerScore, int loserScore);
    }
}
