using TrucoServer.Data.DTOs;
using System.Collections.Generic;

namespace TrucoServer.Helpers.Match
{
    public interface ILobbyRepository
    {
        int ResolveVersionId(baseDatosTrucoEntities context, int maxPlayers);
        Lobby CreateNewLobby(baseDatosTrucoEntities context, User host, int versionId, int maxPlayers, string status);
        void AddLobbyOwner(baseDatosTrucoEntities context, Lobby lobby, User host);
        void CreatePrivateInvitation(baseDatosTrucoEntities context, User host, string matchCode);
        Lobby ResolveLobbyForJoin(baseDatosTrucoEntities context, string matchCode);
        Lobby ResolveLobbyForLeave(baseDatosTrucoEntities context, string matchCode, string username, out User player);
        Lobby FindLobbyByMatchCode(baseDatosTrucoEntities context, string matchCode, bool onlyOpen = true);
        List<PlayerInfo> GetDatabasePlayers(baseDatosTrucoEntities context, Lobby lobby, string ownerUsername);
        string GetLobbyOwnerName(baseDatosTrucoEntities context, int ownerId);
        bool CloseLobbyById(int lobbyId);
        bool ExpireInvitationByMatchCode(string matchCode);
        bool RemoveLobbyMembersById(int lobbyId);
    }
}
