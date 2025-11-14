using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public interface IGameManager
    {
        int SaveMatchToDatabase(string matchCode, List<PlayerInformation> players);
        void SaveDealtCards(string matchCode, PlayerInformation player);
        void SaveRoundResult(string matchCode, string winner);
        void SaveMatchResult(string matchCode, string loserTeam, int winnerScore, int loserScore);
    }
}
