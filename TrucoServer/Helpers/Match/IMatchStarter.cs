using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Match
{
    public interface IMatchStarter
    {
        bool BuildGamePlayersAndCallbacks(List<PlayerInformation> playersList, out List<PlayerInformationWithConstructor> gamePlayers, out Dictionary<int, Contracts.ITrucoCallback> gameCallbacks);
        void InitializeAndRegisterGame(GameInitializationOptions options);
        MatchStartValidation ValidateMatchStart(string matchCode);
        void InitiateMatchSequence(string matchCode, int lobbyId, List<PlayerInformation> players);
        void NotifyMatchStart(string matchCode, List<PlayerInformation> players);
        void HandleMatchStartupCleanup(string matchCode);
        bool GetMatchAndPlayerID(string matchCode, out GameLogic.TrucoMatch match, out int playerID);
        string GetAvatarIdForPlayer(string username);
        string GetOwnerUsername(string matchCode);
    }
}
