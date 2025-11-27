using System;
using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Match
{
    public interface ILobbyCoordinator
    {
        void RegisterLobbyMapping(string matchCode, Lobby lobby);
        void RemoveLobbyMapping(string matchCode);
        bool TryGetLobbyIdFromCode(string matchCode, out int lobbyId);
        object GetOrCreateLobbyLock(int lobbyId);
        string GetMatchCodeFromLobbyId(int lobbyId);
        bool RegisterChatCallback(string matchCode, string player, Contracts.ITrucoCallback callback);
        void RemoveCallbackFromMatch(string matchCode, Contracts.ITrucoCallback callback);
        List<PlayerInfo> GetGuestPlayersFromMemory(string matchCode, string ownerUsername = null);
        PlayerInfo GetPlayerInfoFromCallback(Contracts.ITrucoCallback callback);
        void BroadcastToMatchCallbacksAsync(string matchCode, Action<Contracts.ITrucoCallback> invocation);
        void RemoveInactiveCallbacks(string matchCode);
        void NotifyPlayerJoined(string matchCode, string player);
        void NotifyPlayerLeft(string matchCode, string player);
        void TerminateRunningGameIfExist(string matchCode, string player);
        bool TryGetActiveCallbackForPlayer(string username, out Contracts.ITrucoCallback callback);
        void AddMatchCallbackMapping(Contracts.ITrucoCallback callback, PlayerInfo info);
        void RemoveMatchCallbackMapping(Contracts.ITrucoCallback callback);
        bool TryGetCallbacksSnapshot(string matchCode, out Contracts.ITrucoCallback[] snapshot);
        int GetGuestCountInMemory(string matchCode);
    }
}
