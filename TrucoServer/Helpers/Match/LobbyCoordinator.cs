using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Match
{
    public class LobbyCoordinator : ILobbyCoordinator
    {
        private readonly baseDatosTrucoEntities context;
        private readonly ConcurrentDictionary<string, int> matchCodeToLobbyId = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<int, object> lobbyLocks = new ConcurrentDictionary<int, object>();
        private readonly ConcurrentDictionary<string, List<Contracts.ITrucoCallback>> matchCallbacks = new ConcurrentDictionary<string, List<Contracts.ITrucoCallback>>();
        private static readonly ConcurrentDictionary<Contracts.ITrucoCallback, PlayerInfo> matchCallbackToPlayer = new ConcurrentDictionary<Contracts.ITrucoCallback, PlayerInfo>();

        private const string GUEST_PREFIX = "Guest_";
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";

        public LobbyCoordinator(baseDatosTrucoEntities context)
        {
            this.context = context;
        }

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

        private PlayerInfo GetRegisteredPlayerInfo(int lobbyId, string username)
        {
            var user = context.User.FirstOrDefault(u => u.username == username);

            if (user == null)
            {
                return new PlayerInfo { Username = username, Team = TEAM_1 };
            }

            var member = context.LobbyMember.FirstOrDefault(lm =>
                lm.lobbyID == lobbyId && lm.userID == user.userID);

            return new PlayerInfo
            {
                Username = username,
                Team = member?.team ?? TEAM_1
            };
        }

        public bool RegisterChatCallback(string matchCode, string player, Contracts.ITrucoCallback callback)
        {
            lock (matchCallbacks)
            {
                if (!matchCallbacks.ContainsKey(matchCode))
                {
                    matchCallbacks[matchCode] = new List<Contracts.ITrucoCallback>();
                }

                var existingCallbackIndex = matchCallbacks[matchCode].FindIndex(cb => ReferenceEquals(cb, callback));

                if (existingCallbackIndex >= 0 && matchCallbackToPlayer.TryGetValue(callback, out _))
                {
                    return false;
                    
                }

                PlayerInfo playerInfo = CreatePlayerInfoForChat(matchCode, player);

                if (existingCallbackIndex < 0)
                {
                    matchCallbacks[matchCode].Add(callback);
                }

                matchCallbackToPlayer[callback] = playerInfo;

                return true;
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

            lock (matchCallbacks)
            {
                return callbacks
                    .Where(cb =>
                    {
                        try
                        {
                            return ((ICommunicationObject)cb).State == CommunicationState.Opened;
                        }
                        catch (Exception)
                        {
                            return false;
                            /**
                             * If querying the callback state throws, the client is 
                             * disconnected or the channel is faulted. Return false 
                             * so this callback is excluded from the active players list.
                             */
                        }
                    })
                    .Select(cb => GetPlayerInfoFromCallback(cb))
                    .Where(info => info != null && info.Username.StartsWith(GUEST_PREFIX))
                    .Select(g => new PlayerInfo
                    {
                        Username = g.Username,
                        AvatarId = DEFAULT_AVATAR_ID,
                        OwnerUsername = ownerUsername,
                        Team = g.Team ?? TEAM_1
                    })
                    .ToList();
            }
        }

        public PlayerInfo GetPlayerInfoFromCallback(Contracts.ITrucoCallback callback)
        {
            try
            {
                if (callback == null)
                {
                    return null;
                }

                if (matchCallbackToPlayer.TryGetValue(callback, out PlayerInfo info))
                {
                    return info;
                }
                return null;
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(GetPlayerInfoFromCallback));
                return null;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetPlayerInfoFromCallback));
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetPlayerInfoFromCallback));
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
                lock (matchCallbacks)
                {
                    return callbacks
                        .Where(cb => ((ICommunicationObject)cb).State == CommunicationState.Opened)
                        .Select(cb => GetPlayerInfoFromCallback(cb))
                        .Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX));
                }
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
                Contracts.ITrucoCallback[] snapshot = null;

                lock (matchCallbacks)
                {
                    if (matchCallbacks.TryGetValue(matchCode, out var callbacksList))
                    {
                        snapshot = callbacksList.ToArray();
                    }
                }

                if (snapshot == null)
                {
                    return;
                }

                foreach (var cb in snapshot)
                {
                    Task.Run(() => ProcessSingleCallbackAsync(matchCode, cb, invocation));
                }
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(BroadcastToMatchCallbacksAsync));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(BroadcastToMatchCallbacksAsync));
            }
            catch (OutOfMemoryException ex)
            {
                ServerException.HandleException(ex, nameof(BroadcastToMatchCallbacksAsync));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(BroadcastToMatchCallbacksAsync));
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
                                catch (Exception)
                                {
                                    /**
                                     * Intentionally ignore exceptions during comm.Abort() 
                                     * as this is a best-effort cleanup for inactive callbacks.
                                     * If the communication object is already in a faulted 
                                     * or closed state, Abort() may throw harmless exceptions.
                                     * Propagating these could disrupt the entire RemoveAll 
                                     * operation, affecting other callbacks.
                                     * Since the callback is being removed regardless, silently 
                                     * failing here ensures the cleanup process continues without interruption.
                                     */
                        }

                        return true;
                            }
                            return false;
                        });
                    }
                }
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(RemoveInactiveCallbacks));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(RemoveInactiveCallbacks));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(RemoveInactiveCallbacks));
            }
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
                    catch (Exception) 
                    {
                        /**
                         * Exceptions during comm.Abort() are intentionally ignored 
                         * as this is a cleanup operation for an already inactive callback.
                         * If the communication object is in a faulted or closed 
                         * state, Abort() may throw expected exceptions that do not 
                         * need handling. Since the callback is being removed from 
                         * the list regardless, propagating these errors would be 
                         * unnecessary and could disrupt the process.
                         * This ensures robust callback management without 
                         * interrupting the async processing flow.
                         */
                    }

                    return;
                }
                invocation(cb);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(ProcessSingleCallbackAsync));
                RemoveCallbackSafe(matchCode, cb);
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(ProcessSingleCallbackAsync));
                RemoveCallbackSafe(matchCode, cb);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ProcessSingleCallbackAsync));
                RemoveCallbackSafe(matchCode, cb);
            }
        }

        void RemoveCallbackSafe(string matchCode, Contracts.ITrucoCallback cb)
        {
            lock (matchCallbacks)
            {
                if (matchCallbacks.TryGetValue(matchCode, out var listLocal))
                {
                    listLocal.Remove(cb);
                }
            }
        }

        private int? ResolveLobbyId(string matchCode)
        {
            if (TryGetLobbyIdFromCode(matchCode, out int lobbyId))
            {
                return lobbyId;
            }

            var lobby = new LobbyRepository(context).FindLobbyByMatchCode(matchCode, true);
            return lobby?.lobbyID;
            
        }

        public void NotifyPlayerJoined(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnPlayerJoined(matchCode, player);
                }
                catch (TimeoutException ex)
                {
                    ServerException.HandleException(ex, nameof(NotifyPlayerJoined));
                }
                catch (CommunicationException ex)
                {
                    ServerException.HandleException(ex, nameof(NotifyPlayerJoined));
                }
                catch (Exception ex)
                {
                    ServerException.HandleException(ex, nameof(NotifyPlayerJoined));
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
                catch (TimeoutException ex)
                {
                    ServerException.HandleException(ex, nameof(NotifyPlayerLeft));
                }
                catch (CommunicationException ex)
                {
                    ServerException.HandleException(ex, nameof(NotifyPlayerLeft));
                }
                catch (Exception ex)
                {
                    ServerException.HandleException(ex, nameof(NotifyPlayerLeft));
                }
            });
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
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(TryGetCallbacksSnapshot));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(TryGetCallbacksSnapshot));
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(TryGetCallbacksSnapshot));
                return false;
            }
        }

        public bool TryGetActiveCallbackForPlayer(string username, out Contracts.ITrucoCallback callback)
        {
            callback = null;
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    return false;
                }

                var candidates = matchCallbackToPlayer
                    .Where(kvp => kvp.Value.Username == username)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var cb in candidates)
                {
                    if (cb is ICommunicationObject comm && comm.State == CommunicationState.Opened)
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
            catch (InvalidCastException ex)
            {
                ServerException.HandleException(ex, nameof(TryGetActiveCallbackForPlayer));
                return false;
            }
            catch (CommunicationObjectAbortedException ex)
            {
                ServerException.HandleException(ex, nameof(TryGetActiveCallbackForPlayer));
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                ServerException.HandleException(ex, nameof(TryGetActiveCallbackForPlayer));
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(TryGetActiveCallbackForPlayer));
                return false;
            }
        }

        private PlayerInfo CreatePlayerInfoForChat(string matchCode, string player)
        {
            try
            {
                int? lobbyId = ResolveLobbyId(matchCode);

                if (!lobbyId.HasValue)
                {
                    return new PlayerInfo { Username = player };
                }

                var lobby = context.Lobby.Find(lobbyId.Value);

                if (lobby == null)
                {
                    return new PlayerInfo { Username = player };
                }

                if (player.StartsWith(GUEST_PREFIX))
                {
                    var guestContext = new GuestCreationContext
                    {
                        Lobby = lobby,
                        MatchCode = matchCode,
                        PlayerUsername = player
                    };
                    return CreateGuestPlayerInfo(guestContext);
                }
                else
                {
                    return GetRegisteredPlayerInfo(lobby.lobbyID, player);
                }
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(CreatePlayerInfoForChat));
                return new PlayerInfo { Username = player };
            }

            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(CreatePlayerInfoForChat));
                return new PlayerInfo { Username = player };
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(CreatePlayerInfoForChat));
                return new PlayerInfo { Username = player };
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(CreatePlayerInfoForChat));
                return new PlayerInfo { Username = player };
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CreatePlayerInfoForChat));
                return new PlayerInfo { Username = player };
            }
        }

        private PlayerInfo CreateGuestPlayerInfo(GuestCreationContext creationContext)
        {
            var lobby = creationContext.Lobby;
            var matchCode = creationContext.MatchCode;
            var player = creationContext.PlayerUsername;
            string assignedTeam = TEAM_1;

            try
            {
                int t1CountDb = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_1);
                int t2CountDb = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_2);

                int t1CountMem = 0;
                int t2CountMem = 0;

                if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                {
                    lock (matchCallbacks)
                    {
                        var guestInfos = callbacks
                            .Select(GetPlayerInfoFromCallback)
                            .Where(i => i != null &&
                                i.Username.StartsWith(GUEST_PREFIX) &&
                                i.Username != player &&
                                !string.IsNullOrEmpty(i.Team))
                            .ToList();

                        t1CountMem = guestInfos.Count(i => i.Team == TEAM_1);
                        t2CountMem = guestInfos.Count(i => i.Team == TEAM_2);
                    }
                }

                int t1Total = t1CountDb + t1CountMem;
                int t2Total = t2CountDb + t2CountMem;

                if (lobby.maxPlayers == 2)
                {
                    assignedTeam = (t1Total > 0) ? TEAM_2 : TEAM_1;
                }
                else
                {
                    assignedTeam = (t1Total <= t2Total) ? TEAM_1 : TEAM_2;
                }
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(CreateGuestPlayerInfo));
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(CreateGuestPlayerInfo));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(CreateGuestPlayerInfo));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CreateGuestPlayerInfo));
            }

            return new PlayerInfo
            {
                Username = player,
                Team = assignedTeam,
                AvatarId = DEFAULT_AVATAR_ID
            };
        }
    }
}
