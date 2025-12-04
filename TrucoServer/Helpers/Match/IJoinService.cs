using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Match
{
    public interface IJoinService
    {
        bool ProcessSafeJoin(int lobbyId, string matchCode, string player);
        bool TryJoinAsGuest(baseDatosTrucoEntities context, GuestJoinOptions options);
        bool TryJoinAsUser(baseDatosTrucoEntities context, Lobby lobby, string player);
        bool ValidateJoinConditions(baseDatosTrucoEntities context, Lobby lobby, User playerUser);
        void AddPlayerToLobby(baseDatosTrucoEntities context, Lobby lobby, User playerUser);
        string DetermineTeamForNewPlayer(TeamDeterminationOptions options);
        bool SwitchGuestTeam(string matchCode, string username);
        bool SwitchUserTeam(string matchCode, string username);
        bool CanJoinTeam(string matchCode, string targetTeam);
    }
}
