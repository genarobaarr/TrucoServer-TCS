using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;
using System.ServiceModel;

namespace TrucoServer.Helpers.Match
{
    public class LobbyCoordinator : ILobbyCoordinator
    {
        private readonly ConcurrentDictionary<string, int> matchCodeToLobbyId = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<int, object> lobbyLocks = new ConcurrentDictionary<int, object>();
        private readonly ConcurrentDictionary<string, List<Contracts.ITrucoCallback>> matchCallbacks = new ConcurrentDictionary<string, List<Contracts.ITrucoCallback>>();
        private static readonly ConcurrentDictionary<Contracts.ITrucoCallback, PlayerInfo> matchCallbackToPlayer = new ConcurrentDictionary<Contracts.ITrucoCallback, PlayerInfo>();

        private const string GUEST_PREFIX = "Guest_";
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";

        public void RegisterLobbyMapping(string matchCode, Lobby lobby)
        {
            matchCodeToLobbyId[matchCode] = lobby.lobbyID;
            matchCallbacks.TryAdd(matchCode, new List<Contracts.ITrucoCallback>());
        }

        public void RemoveLobbyMapping(string matchCode)
        {
            matchCodeToLobbyId.TryRemove(matchCode, out _);
        }

        public bool TryGetLobbyIdFromCode(string matchCode, out int lobbyId)
        {
            return matchCodeToLobbyId.TryGetValue(matchCode, out lobbyId);
        }

        public object GetOrCreateLobbyLock(int lobbyId)
        {
            return lobbyLocks.GetOrAdd(lobbyId, (id) => new object());
        }

        public string GetMatchCodeFromLobbyId(int lobbyId)
        {
            return matchCodeToLobbyId.FirstOrDefault(x => x.Value == lobbyId).Key;
        }

        public bool RegisterChatCallback(string matchCode, string player, Contracts.ITrucoCallback callback)
        {
            lock (matchCallbacks)
            {
                if (!matchCallbacks.ContainsKey(matchCode))
                {
                    matchCallbacks[matchCode] = new List<Contracts.ITrucoCallback>();
                }

                if (!matchCallbacks[matchCode].Any(cb => ReferenceEquals(cb, callback)))
                {
                    PlayerInfo playerInfo = CreatePlayerInfoForChat(matchCode, player);
                    matchCallbacks[matchCode].Add(callback);
                    matchCallbackToPlayer[callback] = playerInfo;
                    return true;
                }
                return false;
            }
        }

        public void RemoveCallbackFromMatch(string matchCode, Contracts.ITrucoCallback callback)
        {
            lock (matchCallbacks)
            {
                if (matchCallbacks.ContainsKey(matchCode))
                {
                    matchCallbacks[matchCode].Remove(callback);
                }
            }
        }

        public List<PlayerInfo> GetGuestPlayersFromMemory(string matchCode, string ownerUsername = null)
        {
            if (!matchCallbacks.TryGetValue(matchCode, out var callbacks))
            {
                return new List<PlayerInfo>();
            }

            return callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                .Where(info => info != null && info.Username.StartsWith(GUEST_PREFIX))
                .Select(g => new PlayerInfo { Username = g.Username, AvatarId = DEFAULT_AVATAR_ID, OwnerUsername = ownerUsername, Team = g.Team ?? TEAM_1 })
                .ToList();
        }

        public PlayerInfo GetPlayerInfoFromCallback(Contracts.ITrucoCallback callback)
        {
            try
            {
                if (matchCallbackToPlayer.TryGetValue(callback, out PlayerInfo info))
                {
                    return info;
                }
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPlayerInfoFromCallback));
                return null;
            }
        }

        public void AddMatchCallbackMapping(Contracts.ITrucoCallback callback, PlayerInfo info)
        {
            matchCallbackToPlayer[callback] = info;
        }

        public void RemoveMatchCallbackMapping(Contracts.ITrucoCallback callback)
        {
            matchCallbackToPlayer.TryRemove(callback, out _);
        }

        public int GetGuestCountInMemory(string matchCode)
        {
            if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
            {
                return callbacks.Select(cb => GetPlayerInfoFromCallback(cb)).Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX));
            }
            return 0;
        }

        public void BroadcastToMatchCallbacksAsync(string matchCode, Action<Contracts.ITrucoCallback> invocation)
        {
            if (string.IsNullOrEmpty(matchCode) || invocation == null)
            {
                return;
            }
            try
            {
                if (!matchCallbacks.TryGetValue(matchCode, out var callbacksList))
                {
                    return;
                }
                var snapshot = callbacksList.ToArray();

                foreach (var cb in snapshot)
                {
                    Task.Run(() => ProcessSingleCallbackAsync(matchCode, cb, invocation));
                }
            }
            catch (Exception ex) 
            { 
                LogManager.LogError(ex, nameof(BroadcastToMatchCallbacksAsync)); 
            }
        }

        public void RemoveInactiveCallbacks(string matchCode)
        {
            if (string.IsNullOrEmpty(matchCode))
            {
                return;
            }
            try
            {
                lock (matchCallbacks)
                {
                    if (matchCallbacks.TryGetValue(matchCode, out var list))
                    {
                        list.RemoveAll(cb =>
                        {
                            var comm = (ICommunicationObject)cb;
                            if (comm.State != CommunicationState.Opened)
                            {
                                try 
                                { 
                                    comm.Abort(); 
                                } 
                                catch 
                                { 
                                    /* noop */ 
                                }
                                return true;
                            }
                            return false;
                        });
                    }
                }
            }
            catch (Exception ex) { LogManager.LogError(ex, nameof(RemoveInactiveCallbacks)); }
        }

        private void ProcessSingleCallbackAsync(string matchCode, Contracts.ITrucoCallback cb, Action<Contracts.ITrucoCallback> invocation)
        {
            try
            {
                var comm = (ICommunicationObject)cb;
                if (comm.State != CommunicationState.Opened)
                {
                    lock (matchCallbacks)
                    {
                        if (matchCallbacks.ContainsKey(matchCode))
                        {
                            matchCallbacks[matchCode].Remove(cb);
                        }
                    }
                    try 
                    { 
                        comm.Abort(); 
                    } 
                    catch 
                    {
                        /* noop */ 
                    }
                    return;
                }
                invocation(cb);
            }
            catch (InvalidCastException ex)
            {
                LogManager.LogError(ex, $"{nameof(ProcessSingleCallbackAsync)} - Invalid Callback Cast");
                lock (matchCallbacks)
                {
                    if (matchCallbacks.TryGetValue(matchCode, out var listLocal))
                    {
                        listLocal.Remove(cb);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ProcessSingleCallbackAsync));
            }
        }

        public void NotifyPlayerJoined(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try 
                { 
                    cb.OnPlayerJoined(matchCode, player); 
                }
                catch (Exception ex) 
                { 
                    LogManager.LogError(ex, nameof(NotifyPlayerJoined)); 
                }
            });
        }

        public void NotifyPlayerLeft(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try 
                { 
                    cb.OnPlayerLeft(matchCode, player); 
                }
                catch (Exception ex) 
                {
                    LogManager.LogError(ex, nameof(NotifyPlayerLeft)); 
                }
            });
        }

        public void TerminateRunningGameIfExist(string matchCode, string player)
        {
            /* 
             * GameRegistry handles running games; coordinator just invokes registry if needed.
             * Keep an entry point here if service wants to call it via coordinator in future.
             * Implementation left intentionally blank for separation of concerns.
             */
        }

        public bool TryGetCallbacksSnapshot(string matchCode, out Contracts.ITrucoCallback[] snapshot)
        {
            snapshot = null;
            try
            {
                if (!matchCallbacks.TryGetValue(matchCode, out var callbacksList))
                {
                    return false;
                }
                snapshot = callbacksList.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(TryGetCallbacksSnapshot));
                return false;
            }
        }

        public bool TryGetActiveCallbackForPlayer(string username, out Contracts.ITrucoCallback callback)
        {
            callback = null;
            try
            {
                var candidates = matchCallbackToPlayer.Where(kvp => kvp.Value.Username == username).Select(kvp => kvp.Key).ToList();
                foreach (var cb in candidates)
                {
                    if (((ICommunicationObject)cb).State == CommunicationState.Opened)
                    {
                        callback = cb;
                        return true;
                    }
                    else
                    {
                        matchCallbackToPlayer.TryRemove(cb, out _);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(TryGetActiveCallbackForPlayer));
                return false;
            }
        }

        private PlayerInfo CreatePlayerInfoForChat(string matchCode, string player)
        {
            if (player.StartsWith(GUEST_PREFIX))
            {
                string assignedTeam = TEAM_1;
                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = new LobbyRepository().FindLobbyByMatchCode(context, matchCode, true);
                    if (lobby != null && lobby.maxPlayers > 2)
                    {
                        int t1Count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_1);
                        int t2Count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_2);

                        if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                        {
                            t1Count += callbacks.Select(GetPlayerInfoFromCallback).Count(i => i != null && i.Team == TEAM_1);
                            t2Count += callbacks.Select(GetPlayerInfoFromCallback).Count(i => i != null && i.Team == TEAM_2);
                        }
                        if (t1Count >= t2Count)
                        {
                            assignedTeam = TEAM_2;
                        }
                    }
                }
                return new PlayerInfo 
                { 
                    Username = player, 
                    Team = assignedTeam, 
                    AvatarId = DEFAULT_AVATAR_ID 
                };
            }
            return new PlayerInfo { Username = player };
        }
    }
}
