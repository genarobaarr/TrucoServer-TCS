using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Match
{
    public interface IJoinService
    {
        bool ProcessSafeJoin(int lobbyId, string matchCode, string player);
        bool TryJoinAsGuest(GuestJoinOptions options);
        bool TryJoinAsUser(Lobby lobby, string player);
        bool ValidateJoinConditions(Lobby lobby, User playerUser);
        void AddPlayerToLobby(Lobby lobby, User playerUser);
        string DetermineTeamForNewPlayer(TeamDeterminationOptions options);
        bool SwitchGuestTeam(string matchCode, string username);
        bool SwitchUserTeam(string matchCode, string username);
        bool CanJoinTeam(string matchCode, string targetTeam);
    }
}
