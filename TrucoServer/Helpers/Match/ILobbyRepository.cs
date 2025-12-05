using TrucoServer.Data.DTOs;
using System.Collections.Generic;

namespace TrucoServer.Helpers.Match
{
    public interface ILobbyRepository
    {
        int ResolveVersionId(baseDatosTrucoEntities context, int maxPlayers);
        Lobby CreateNewLobby(baseDatosTrucoEntities context, LobbyCreationOptions options);
        void AddLobbyOwner(baseDatosTrucoEntities context, Lobby lobby, User host);
        bool IsPlayerInLobby(baseDatosTrucoEntities context, int lobbyId, int userId);
        TeamCountsResult GetTeamCounts(baseDatosTrucoEntities context, int lobbyId);
        void AddMember(baseDatosTrucoEntities context, int lobbyId, int userId, string role, string team);
        void CreatePrivateInvitation(baseDatosTrucoEntities context, User host, string matchCode);
        Lobby ResolveLobbyForJoin(baseDatosTrucoEntities context, string matchCode);
        LobbyLeaveResult ResolveLobbyForLeave(baseDatosTrucoEntities context, LobbyLeaveCriteria criteria);
        Lobby FindLobbyByMatchCode(baseDatosTrucoEntities context, string matchCode, bool onlyOpen = true);
        List<PlayerInfo> GetDatabasePlayers(baseDatosTrucoEntities context, Lobby lobby, string ownerUsername);
        string GetLobbyOwnerName(baseDatosTrucoEntities context, int ownerId);
        bool CloseLobbyById(int lobbyId);
        bool ExpireInvitationByMatchCode(string matchCode);
        bool RemoveLobbyMembersById(int lobbyId);
    }
}
