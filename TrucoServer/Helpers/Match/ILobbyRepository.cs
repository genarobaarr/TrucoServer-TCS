using TrucoServer.Data.DTOs;
using System.Collections.Generic;

namespace TrucoServer.Helpers.Match
{
    public interface ILobbyRepository
    {
        int ResolveVersionId(int maxPlayers);
        Lobby CreateNewLobby(LobbyCreationOptions options);
        void AddLobbyOwner(Lobby lobby, User host);
        bool IsPlayerInLobby(int lobbyId, int userId);
        TeamCountsResult GetTeamCounts(int lobbyId);
        void AddMember(LobbyMemberDetails memberDetails);
        Lobby ResolveLobbyForJoin(string matchCode);
        LobbyLeaveResult ResolveLobbyForLeave(LobbyLeaveCriteria criteria);
        Lobby FindLobbyByMatchCode(string matchCode, bool onlyOpen = true);
        List<PlayerInformation> GetDatabasePlayers(Lobby lobby, string ownerUsername);
        string GetLobbyOwnerName(int ownerId);
        bool CloseLobbyById(int lobbyId);
        bool ExpireInvitationByMatchCode(string matchCode);
        bool RemoveLobbyMembersById(int lobbyId);
    }
}
