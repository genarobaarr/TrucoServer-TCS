using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Match
{
    public interface IMatchStarter
    {
        bool BuildGamePlayersAndCallbacks(List<PlayerInfo> playersList, out List<PlayerInformation> gamePlayers, out Dictionary<int, Contracts.ITrucoCallback> gameCallbacks);
        void InitializeAndRegisterGame(string matchCode, int lobbyId, List<PlayerInformation> gamePlayers, Dictionary<int, Contracts.ITrucoCallback> gameCallbacks);
        void NotifyMatchStart(string matchCode, List<PlayerInfo> players);
        void HandleMatchStartupCleanup(string matchCode);
        bool GetMatchAndPlayerID(string matchCode, out GameLogic.TrucoMatch match, out int playerID);
        string GetAvatarIdForPlayer(string username);
        string GetOwnerUsername(string matchCode);
    }
}
