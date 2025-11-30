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

                var existingCallbackIndex = matchCallbacks[matchCode].FindIndex(cb => ReferenceEquals(cb, callback));

                if (existingCallbackIndex >= 0)
                {
                    if (matchCallbackToPlayer.TryGetValue(callback, out var existingInfo))
                    {
                        return false;
                    }
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
                        catch
                        {
                            return false;
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
            try
            {
                int lobbyId;

                if (!TryGetLobbyIdFromCode(matchCode, out lobbyId))
                {
                    using (var context = new baseDatosTrucoEntities())
                    {
                        var lobby = new LobbyRepository().FindLobbyByMatchCode(context, matchCode, true);

                        if (lobby != null)
                        {
                            lobbyId = lobby.lobbyID;
                        }
                        else
                        {
                            return new PlayerInfo { Username = player };
                        }
                    }
                }

                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = context.Lobby.Find(lobbyId);

                    if (lobby == null)
                    {
                        return new PlayerInfo { Username = player };
                    }

                    if (player.StartsWith(GUEST_PREFIX))
                    {
                        return CreateGuestPlayerInfo(context, lobby, matchCode, player);
                    }

                    var user = context.User.FirstOrDefault(u => u.username == player);

                    if (user != null)
                    {
                        var member = context.LobbyMember.FirstOrDefault(lm =>
                            lm.lobbyID == lobby.lobbyID && lm.userID == user.userID);

                        if (member != null)
                        {
                            return new PlayerInfo
                            {
                                Username = player,
                                Team = member.team
                            };
                        }
                        else
                        {
                            return new PlayerInfo
                            {
                                Username = player,
                                Team = TEAM_1
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CreatePlayerInfoForChat));
            }

            return new PlayerInfo { Username = player };
        }

        private PlayerInfo CreateGuestPlayerInfo(baseDatosTrucoEntities context, Lobby lobby, string matchCode, string player)
        {
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateGuestPlayerInfo)} error");
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
