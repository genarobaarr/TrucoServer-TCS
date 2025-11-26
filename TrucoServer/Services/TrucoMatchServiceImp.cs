using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Utilities;

namespace TrucoServer.Services
{
    public class TrucoMatchServiceImp : ITrucoMatchService
    {
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";
        private const string EXPIRED_STATUS = "Expired";
        private const string PENDING_STATUS = "Pending";
        private const string PUBLIC_STATUS = "Public";
        private const string PRIVATE_STATUS = "Private";
        private const string CLOSED_STATUS = "Closed";
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";
        private const string GUEST_PREFIX = "Guest_";
        private const string ROLE_OWNER = "Owner";
        private const string ROLE_PLAYER = "Player";
        private const int MATCH_CODE_LENGTH = 6;

        private readonly ConcurrentDictionary<string, int> matchCodeToLobbyId = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<int, object> lobbyLocks = new ConcurrentDictionary<int, object>();
        private readonly ConcurrentDictionary<string, List<ITrucoCallback>> matchCallbacks = new ConcurrentDictionary<string, List<ITrucoCallback>>();
        private static readonly ConcurrentDictionary<ITrucoCallback, PlayerInfo> matchCallbackToPlayer = new ConcurrentDictionary<ITrucoCallback, PlayerInfo>();
        private readonly ConcurrentDictionary<string, TrucoMatch> runningGames = new ConcurrentDictionary<string, TrucoMatch>();

        private readonly IGameManager gameManager = new TrucoGameManager();
        private readonly IDeckShuffler shuffler = new DefaultDeckShuffler();

        public string CreateLobby(string hostUsername, int maxPlayers, string privacy)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var host = context.User.FirstOrDefault(u => u.username == hostUsername)
                        ?? throw new InvalidOperationException("Host user not found.");

                    int versionId = ResolveVersionId(context, maxPlayers);
                    string matchCode = GenerateMatchCode();

                    string normalizedStatus =
                        privacy.Equals("public", StringComparison.OrdinalIgnoreCase)
                        ? PUBLIC_STATUS : PRIVATE_STATUS;

                    var lobby = CreateNewLobby(context, host, versionId, maxPlayers, normalizedStatus);

                    AddLobbyOwner(context, lobby, host);

                    if (lobby.status.Equals(PRIVATE_STATUS, StringComparison.OrdinalIgnoreCase))
                    {
                        CreatePrivateInvitation(context, host, matchCode);
                    }

                    context.SaveChanges();
                    RegisterLobbyMapping(matchCode, lobby);
                    lobbyLocks.TryAdd(lobby.lobbyID, new object());

                    Console.WriteLine($"[SERVER] Lobby created by {hostUsername}, code={matchCode}, privacy={privacy}, maxPlayers={maxPlayers}.");
                    return matchCode;
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateLobby)} - Business Logic Error (Host not found)");
                return string.Empty;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateLobby)} - DataBase Saving Error");
                return string.Empty;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateLobby)} - SQL Server Error");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CreateLobby));
                return string.Empty;
            }
        }

        public bool JoinMatch(string matchCode, string player)
        {
            bool joinSuccess = false;
            Lobby lobby = null;

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    lobby = ResolveLobbyForJoin(context, matchCode);

                    if (lobby == null || lobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[JOIN] Denied: Lobby closed or not found for code {matchCode}.");
                        return false;
                    }
                }

                object lobbyLock = lobbyLocks.GetOrAdd(lobby.lobbyID, (id) => new object());

                lock (lobbyLock)
                {
                    joinSuccess = ProcessSafeJoin(lobby.lobbyID, matchCode, player);
                }

                if (joinSuccess)
                {
                    Console.WriteLine($"[SERVER] {player} was processed for lobby {matchCode}.");
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(JoinMatch)} - DataBase Saving Error");
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(JoinMatch)} - SQL Server Error");
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(JoinMatch)} - Callback Communication Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(JoinMatch));
            }

            return joinSuccess;
        }

        public void LeaveMatch(string matchCode, string player)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = ResolveLobbyForLeave(context, matchCode, player, out User username);

                    if (lobby == null || username == null)
                    {
                        return;
                    }

                    RemovePlayerFromLobby(context, lobby, username);
                    NotifyPlayerLeft(matchCode, player);
                    HandleEmptyLobbyCleanup(context, lobby, matchCode);
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(LeaveMatch)} - DataBase Deletion Error");
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(LeaveMatch)} - SQL Server Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(LeaveMatch));
            }
        }

        public void StartMatch(string matchCode)
        {
            try
            {
                ValidatedLobbyData validatedData;

                using (var context = new baseDatosTrucoEntities())
                {
                    validatedData = GetAndValidateLobbyForStart(context, matchCode);

                    if (validatedData == null)
                    {
                        LogManager.LogWarn($"StartMatch validation failed for {matchCode}", nameof(StartMatch));
                        return;
                    }
                }

                List<PlayerInfo> playersList = GetLobbyPlayers(matchCode);

                if (!BuildGamePlayersAndCallbacks(playersList, out var gamePlayers, out var gameCallbacks))
                {
                    return;
                }

                var newDeck = new Deck(shuffler);
                var newGame = new TrucoMatch(matchCode, validatedData.Lobby.lobbyID, gamePlayers, gameCallbacks, newDeck, gameManager);

                if (!runningGames.TryAdd(matchCode, newGame))
                {
                    LogManager.LogError(new Exception($"Failed to add running game {matchCode}"), nameof(StartMatch));
                    return;
                }

                NotifyMatchStart(matchCode, playersList);
                HandleMatchStartupCleanup(matchCode);

                Task.Delay(500).Wait();
                newGame.StartNewHand();
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(StartMatch)} - Callback Communication Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(StartMatch));
            }
        }

        public List<PublicLobbyInfo> GetPublicLobbies()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var publicLobbies = context.Lobby
                        .Where(l => l.status == PUBLIC_STATUS)
                        .Select(l => new
                        {
                            l.lobbyID,
                            l.maxPlayers,
                            l.createdAt,
                            l.ownerID
                        })
                        .ToList();

                    var result = new List<PublicLobbyInfo>();

                    foreach (var lobby in publicLobbies)
                    {
                        int currentPlayers = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
                        var owner = context.User.FirstOrDefault(u => u.userID == lobby.ownerID);

                        string matchCode = matchCodeToLobbyId.FirstOrDefault(kvp => kvp.Value == lobby.lobbyID).Key ?? "(unknown)";

                        result.Add(new PublicLobbyInfo
                        {
                            MatchCode = matchCode,
                            MatchName = $"{owner?.username ?? "Host"} - {lobby.maxPlayers}P",
                            CurrentPlayers = currentPlayers,
                            MaxPlayers = lobby.maxPlayers
                        });
                    }

                    return result;
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPublicLobbies)} - SQL Server Error");
                return new List<PublicLobbyInfo>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(GetPublicLobbies));
                return new List<PublicLobbyInfo>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPublicLobbies));
                return new List<PublicLobbyInfo>();
            }
        }

        public void JoinMatchChat(string matchCode, string player)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                RemoveInactiveCallbacks(matchCode);

                bool isNewCallback = false;

                lock (matchCallbacks)
                {
                    if (!matchCallbacks.ContainsKey(matchCode))
                    {
                        matchCallbacks[matchCode] = new List<ITrucoCallback>();
                    }

                    if (!matchCallbacks[matchCode].Any(cb => ReferenceEquals(cb, callback)))
                    {
                        PlayerInfo playerInfo = CreatePlayerInfoForChat(matchCode, player);

                        matchCallbacks[matchCode].Add(callback);
                        matchCallbackToPlayer[callback] = playerInfo;
                        isNewCallback = true;
                    }
                }

                Console.WriteLine($"[CHAT] {player} joined the lobby {matchCode}.");

                if (isNewCallback)
                {
                    NotifyPlayerJoined(matchCode, player);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(JoinMatchChat)} - There is no WCF Operational Context");
            }
            catch (OutOfMemoryException ex)
            {
                LogManager.LogError(ex, $"{nameof(JoinMatchChat)} - Insuficient Memory");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(JoinMatchChat));
            }
        }

        public void LeaveMatchChat(string matchCode, string player)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                lock (matchCallbacks)
                {
                    if (matchCallbacks.ContainsKey(matchCode))
                    {
                        matchCallbacks[matchCode].Remove(callback);
                    }
                }

                if (matchCallbacks.ContainsKey(matchCode))
                {
                    BroadcastToMatchCallbacksAsync(matchCode, cb =>
                    {
                        try
                        {
                            cb.OnPlayerLeft(matchCode, player);
                        }
                        catch (CommunicationException ex)
                        {
                            LogManager.LogError(ex, $"{nameof(LeaveMatchChat)} - Callback Communication Error");
                        }
                        catch (TimeoutException ex)
                        {
                            LogManager.LogError(ex, $"{nameof(LeaveMatchChat)} - Timeout When Notifying Exit");
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogError(ex, nameof(LeaveMatchChat));
                        }
                    });
                }
                Console.WriteLine($"[CHAT] {player} left the lobby {matchCode}.");

                if (runningGames.TryGetValue(matchCode, out TrucoMatch match))
                {
                    match.AbortMatch(player);
                    runningGames.TryRemove(matchCode, out _);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(LeaveMatchChat)} - There is no WCF Operational Context");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(LeaveMatchChat));
            }
        }

        public void SendChatMessage(string matchCode, string player, string message)
        {
            try
            {
                RemoveInactiveCallbacks(matchCode);

                if (!matchCallbacks.ContainsKey(matchCode))
                {
                    return;
                }

                var senderCallback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                BroadcastToMatchCallbacksAsync(matchCode, cb =>
                {
                    try
                    {
                        if (!ReferenceEquals(cb, senderCallback))
                        {
                            cb.OnChatMessage(matchCode, player, message);
                        }
                    }
                    catch (CommunicationException ex)
                    {
                        LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Disconnected Client");
                    }
                    catch (TimeoutException ex)
                    {
                        LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Timeout When Sending Message");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Callback Error");
                    }
                });

                Console.WriteLine($"[{matchCode}] {player}: {message}");
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Invalid OperationContext");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SendChatMessage));
            }
        }

        public void PlayCard(string matchCode, string cardFileName)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.PlayCard(playerID, cardFileName);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(PlayCard));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(PlayCard));
            }
        }

        public void CallTruco(string matchCode, string betType)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.CallTruco(playerID, betType);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(CallTruco));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CallTruco));
            }
        }

        public void RespondToCall(string matchCode, string response)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.RespondToCall(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(RespondToCall));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RespondToCall));
            }
        }

        public void CallEnvido(string matchCode, string betType)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.CallEnvido(playerID, betType);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(CallEnvido));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CallEnvido));
            }
        }

        public void RespondToEnvido(string matchCode, string response)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.RespondToEnvido(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(RespondToEnvido));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RespondToEnvido));
            }
        }

        public void CallFlor(string matchCode, string betType)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.CallFlor(playerID, betType);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(CallFlor));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CallFlor));
            }
        }

        public void RespondToFlor(string matchCode, string response)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.RespondToFlor(playerID, response);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(RespondToFlor));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RespondToFlor));
            }
        }

        public void GoToDeck(string matchCode)
        {
            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    match.PlayerGoesToDeck(playerID);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(GoToDeck));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GoToDeck));
            }
        }

        public void SwitchTeam(string matchCode, string username)
        {
            try
            {
                bool switchSuccess = false;

                if (username.StartsWith(GUEST_PREFIX))
                {
                    switchSuccess = SwitchGuestTeam(matchCode, username);
                }
                else
                {
                    switchSuccess = SwitchUserTeam(matchCode, username);
                }

                if (switchSuccess)
                {
                    BroadcastToMatchCallbacksAsync(matchCode, cb => cb.OnPlayerJoined(matchCode, username));
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(SwitchTeam)} - Error Saving DataBase");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SwitchTeam));
            }
        }

        public List<PlayerInfo> GetLobbyPlayers(string matchCode)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = FindLobbyByMatchCode(context, matchCode, false);

                    if (lobby == null)
                    {
                        return GetGuestPlayersFromMemory(matchCode);
                    }

                    string ownerUsername = GetLobbyOwnerName(context, lobby.ownerID);

                    var dbPlayers = GetDatabasePlayers(context, lobby, ownerUsername);

                    var guestPlayers = GetGuestPlayersFromMemory(matchCode, ownerUsername);

                    MergePlayersLists(dbPlayers, guestPlayers);

                    return dbPlayers;
                }
            }
            catch (FormatException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLobbyPlayers)} - Code Format Error");
                return new List<PlayerInfo>();
            }
            catch (OverflowException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLobbyPlayers)} - Code Out of Range");
                return new List<PlayerInfo>();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLobbyPlayers)} - SQL Server Error");
                return new List<PlayerInfo>();
            }
            catch (NotSupportedException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLobbyPlayers)} - LINQ Not Supported");
                return new List<PlayerInfo>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLobbyPlayers)} - Invalid Operation (DataBase Context)");
                return new List<PlayerInfo>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetLobbyPlayers));
                return new List<PlayerInfo>();
            }
        }

        private static int ResolveVersionId(baseDatosTrucoEntities context, int maxPlayers)
        {
            try
            {
                int versionId = context.Versions
                    .Where(v => v.configuration.Contains(maxPlayers == 2 ? "1v1" : "2v2"))
                    .Select(v => v.versionID)
                    .FirstOrDefault();

                return versionId == 0 && context.Versions.Any()
                    ? context.Versions.First().versionID
                    : versionId;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveVersionId)} - SQL Server Error");
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveVersionId)} - Invalid Operation (DataBase Context)");
                return 0;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveVersionId));
                return 0;
            }
        }

        private Lobby CreateNewLobby(baseDatosTrucoEntities context, User host, int versionId, int maxPlayers, string status)
        {
            try
            {
                var newLobby = new Lobby
                {
                    ownerID = host.userID,
                    versionID = versionId,
                    maxPlayers = maxPlayers,
                    status = status,
                    createdAt = DateTime.Now
                };

                context.Lobby.Add(newLobby);
                context.SaveChanges();

                return newLobby;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateNewLobby)} - DataBase Saving Error");
                return null;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateNewLobby)} - SQL Server Error");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CreateNewLobby));
                return null;
            }
        }

        private void AddLobbyOwner(baseDatosTrucoEntities context, Lobby lobby, User host)
        {
            try
            {
                context.LobbyMember.Add(new LobbyMember
                {
                    lobbyID = lobby.lobbyID,
                    userID = host.userID,
                    role = ROLE_OWNER,
                    team = TEAM_1
                });
            }
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, $"{nameof(AddLobbyOwner)} - Null Argument");
                throw;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(AddLobbyOwner)} - SQL Server Error on Query");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AddLobbyOwner));
                throw;
            }
        }

        private void CreatePrivateInvitation(baseDatosTrucoEntities context, User host, string matchCode)
        {
            try
            {
                int numericCode = GenerateNumericCodeFromString(matchCode);

                var previousInvitations = context.Invitation
                    .Where(i => i.senderID == host.userID && i.status == PENDING_STATUS)
                    .ToList();

                foreach (var inv in previousInvitations)
                {
                    inv.status = EXPIRED_STATUS;
                    inv.expiresAt = DateTime.Now;
                }

                context.Invitation.Add(new Invitation
                {
                    senderID = host.userID,
                    receiverEmail = null,
                    code = numericCode,
                    status = PENDING_STATUS,
                    expiresAt = DateTime.Now.AddHours(2)
                });
            }
            catch (FormatException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreatePrivateInvitation)} - Code Format Error");
                throw;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreatePrivateInvitation)} - SQL Server Error on Query");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CreatePrivateInvitation));
                throw;
            }
        }

        private void RegisterLobbyMapping(string matchCode, Lobby lobby)
        {
            matchCodeToLobbyId[matchCode] = lobby.lobbyID;
            matchCallbacks.TryAdd(matchCode, new List<ITrucoCallback>());
        }

        private Lobby ResolveLobbyForJoin(baseDatosTrucoEntities context, string matchCode)
        {
            try
            {
                if (matchCodeToLobbyId.TryGetValue(matchCode, out int lobbyId))
                {
                    var lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == lobbyId);

                    if (lobby != null && !lobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
                    {
                        return lobby;
                    }

                    Console.WriteLine($"[SECURITY] Lobby {lobbyId} ({matchCode}) is closed or invalid.");
                    matchCodeToLobbyId.TryRemove(matchCode, out _);
                    return null;
                }

                int numericCode = GenerateNumericCodeFromString(matchCode);
                var invitation = context.Invitation.FirstOrDefault(i =>
                    i.code == numericCode &&
                    i.status == PENDING_STATUS &&
                    i.expiresAt > DateTime.Now);

                if (invitation == null)
                {
                    Console.WriteLine($"[SECURITY] No valid invitation found for {matchCode}.");
                    return null;
                }

                var lobbyCandidate = context.Lobby.FirstOrDefault(l =>
                    l.ownerID == invitation.senderID &&
                    !l.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase));

                if (lobbyCandidate == null)
                {
                    Console.WriteLine($"[SECURITY] Invitation {matchCode} points to closed or missing lobby.");
                    return null;
                }

                matchCodeToLobbyId.TryAdd(matchCode, lobbyCandidate.lobbyID);

                return lobbyCandidate;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveLobbyForJoin)} - SQL Server Error");
                return null;
            }
            catch (FormatException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveLobbyForJoin)} - Code Format Error");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveLobbyForJoin)} - Invalid Operation (DataBase Context)");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveLobbyForJoin));
                return null;
            }
        }

        private bool ProcessSafeJoin(int lobbyId, string matchCode, string player)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var freshLobby = context.Lobby.Find(lobbyId);

                if (freshLobby == null || freshLobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[JOIN] Denied: Lobby closed while waiting for lock.");
                    return false;
                }

                bool isGuest = player.StartsWith(GUEST_PREFIX);

                if (isGuest)
                {
                    return TryJoinAsGuest(context, freshLobby, matchCode, player);
                }
                else
                {
                    return TryJoinAsUser(context, freshLobby, player);
                }
            }
        }

        private bool TryJoinAsGuest(baseDatosTrucoEntities context, Lobby lobby, string matchCode, string player)
        {
            if (!lobby.status.Equals(PUBLIC_STATUS, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[JOIN] Denied: Guest player {player} tried to join a private lobby.");
                return false;
            }

            int currentDbPlayers = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
            int guestCount = 0;

            if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
            {
                guestCount = callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                                      .Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX));
            }

            if ((currentDbPlayers + guestCount) >= lobby.maxPlayers)
            {
                Console.WriteLine($"[JOIN] Denied: Public lobby {lobby.lobbyID} is full.");
                return false;
            }

            Console.WriteLine($"[SERVER] Guest player {player} approved for public lobby {matchCode}.");
            return true;
        }

        private bool TryJoinAsUser(baseDatosTrucoEntities context, Lobby lobby, string player)
        {
            User playerUser = context.User.FirstOrDefault(u => u.username == player);

            if (!ValidateJoinConditions(context, lobby, playerUser))
            {
                return false;
            }

            AddPlayerToLobby(context, lobby, playerUser);
            return true;
        }

        private static bool ValidateJoinConditions(baseDatosTrucoEntities context, Lobby lobby, User playerUser)
        {
            if (playerUser == null)
            {
                Console.WriteLine($"[JOIN] Player not found.");
                return false;
            }

            if (lobby == null || lobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[JOIN] Denied: Lobby {lobby.lobbyID} is closed.");
                return false;
            }

            try
            {
                int count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);

                if (count >= lobby.maxPlayers)
                {
                    Console.WriteLine($"[JOIN] Lobby {lobby.lobbyID} is full ({count}/{lobby.maxPlayers}).");
                    return false;
                }

                return true;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(ValidateJoinConditions)} - SQL Server Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ValidateJoinConditions));
                return false;
            }
        }

        private void AddPlayerToLobby(baseDatosTrucoEntities context, Lobby lobby, User playerUser)
        {
            try
            {
                if (context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID && lm.userID == playerUser.userID))
                {
                    return;
                }

                int team1Count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_1);
                int team2Count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_2);

                string assignedTeam = DetermineTeamForNewPlayer(lobby.maxPlayers, team1Count, team2Count, playerUser.username);

                context.LobbyMember.Add(new LobbyMember
                {
                    lobbyID = lobby.lobbyID,
                    userID = playerUser.userID,
                    role = ROLE_PLAYER,
                    team = assignedTeam
                });

                context.SaveChanges();
                Console.WriteLine($"[JOIN] Player '{playerUser.username}' added to lobby {lobby.lobbyID}.");
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(AddPlayerToLobby)} - DataBase Saving Error");
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(AddPlayerToLobby)} - SQL Server Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AddPlayerToLobby));
            }
        }

        private static string DetermineTeamForNewPlayer(int maxPlayers, int team1Count, int team2Count, string username)
        {
            if (maxPlayers == 2)
            {
                string team = (team1Count <= team2Count) ? TEAM_1 : TEAM_2;

                Console.WriteLine($"[JOIN] 1v1 match detected, assigning player {username} to {team}.");
                return team;
            }

            return (team1Count > team2Count) ? TEAM_2 : TEAM_1;
        }

        private void NotifyPlayerJoined(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnPlayerJoined(matchCode, player);
                }
                catch (CommunicationException ex)
                {
                    LogManager.LogError(ex, $"{nameof(NotifyPlayerJoined)} - Callback Communication Error");
                }
                catch (TimeoutException ex)
                {
                    LogManager.LogError(ex, $"{nameof(NotifyPlayerJoined)} - Timeout Error");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, nameof(NotifyPlayerJoined));
                }
            });
        }

        private Lobby ResolveLobbyForLeave(baseDatosTrucoEntities context, string matchCode, string username, out User player)
        {
            try
            {
                player = context.User.FirstOrDefault(u => u.username == username);

                if (player == null)
                {
                    return null;
                }

                return FindLobbyByMatchCode(context, matchCode, true);
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveLobbyForLeave)} - SQL Server Error");
                player = null;
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveLobbyForLeave)} - Invalid Operation (DataBase Context)");
                player = null;
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveLobbyForLeave));
                player = null;
                return null;
            }
        }

        private static void RemovePlayerFromLobby(baseDatosTrucoEntities context, Lobby lobby, User player)
        {
            try
            {
                var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobby.lobbyID && lm.userID == player.userID);

                if (member != null)
                {
                    context.LobbyMember.Remove(member);
                    context.SaveChanges();
                    Console.WriteLine($"[LEAVE] Player '{player.username}' removed from lobby {lobby.lobbyID}.");
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemovePlayerFromLobby)} - SQL Server Error");
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemovePlayerFromLobby)} - Invalid Operation (DataBase Context)");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RemovePlayerFromLobby));
            }
        }

        private void NotifyPlayerLeft(string matchCode, string username)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnPlayerLeft(matchCode, username);
                }
                catch (CommunicationException ex)
                {
                    LogManager.LogError(ex, $"{nameof(NotifyPlayerLeft)} - Callback Communication Error");
                }
                catch (TimeoutException ex)
                {
                    LogManager.LogError(ex, $"{nameof(NotifyPlayerLeft)} - Timeout Error");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, nameof(NotifyPlayerLeft));
                }
            });
        }

        private void HandleEmptyLobbyCleanup(baseDatosTrucoEntities context, Lobby lobby, string matchCode)
        {
            try
            {
                bool isEmpty = !context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID);

                if (isEmpty)
                {
                    bool closed = CloseLobbyById(lobby.lobbyID);
                    bool expired = ExpireInvitationByMatchCode(matchCode);
                    bool removed = RemoveLobbyMembersById(lobby.lobbyID);

                    Console.WriteLine($"[CLEANUP] Lobby {lobby.lobbyID} cleanup result: closedLobby={closed}, expiredInvitation={expired}, removedMembers={removed}");

                    matchCodeToLobbyId.TryRemove(matchCode, out _);
                    lobbyLocks.TryRemove(lobby.lobbyID, out _);
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(HandleEmptyLobbyCleanup)} - SQL Server Error on Check");
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(HandleEmptyLobbyCleanup)} - Invalid Operation (DataBase Context)");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(HandleEmptyLobbyCleanup));
            }
        }

        private ValidatedLobbyData GetAndValidateLobbyForStart(baseDatosTrucoEntities context, string matchCode)
        {
            try
            {
                var lobby = FindLobbyByMatchCode(context, matchCode, true);

                if (lobby == null)
                {
                    Console.WriteLine($"[START FAILED] Lobby not found for code {matchCode}");
                    return null;
                }

                var dbMembers = context.LobbyMember.Where(lm => lm.lobbyID == lobby.lobbyID).ToList();

                int guestCount = 0;
                List<PlayerInfo> guestInfos = new List<PlayerInfo>();

                if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                {
                    guestInfos = callbacks
                        .Select(cb => GetPlayerInfoFromCallback(cb))
                        .Where(info => info != null && info.Username != null && info.Username.StartsWith(GUEST_PREFIX))
                        .ToList();

                    guestCount = guestInfos.Count;
                }

                if ((dbMembers.Count + guestCount) != lobby.maxPlayers)
                {
                    Console.WriteLine($"[START FAILED] Lobby {matchCode} is not full (DB:{dbMembers.Count}, Guest:{guestCount} / Total:{lobby.maxPlayers}).");
                    return null;
                }

                int team1Count = dbMembers.Count(m => m.team == TEAM_1) + guestInfos.Count(g => string.Equals(g.Team, TEAM_1, StringComparison.OrdinalIgnoreCase));
                int team2Count = dbMembers.Count(m => m.team == TEAM_2) + guestInfos.Count(g => string.Equals(g.Team, TEAM_2, StringComparison.OrdinalIgnoreCase));

                if (lobby.maxPlayers > 2)
                {
                    if (team1Count != team2Count)
                    {
                        Console.WriteLine($"[START FAILED] Teams are unbalanced in {matchCode} (T1:{team1Count} vs T2:{team2Count}).");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine($"[INFO] Skipping team balance check for 1v1 match {matchCode}.");
                }

                return new ValidatedLobbyData
                {
                    Lobby = lobby,
                    Members = dbMembers
                };
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetAndValidateLobbyForStart)} - SQL Server Error");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetAndValidateLobbyForStart)} - Invalid Operation (Data Context)");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetAndValidateLobbyForStart));
                return null;
            }
        }

        private bool BuildGamePlayersAndCallbacks(List<PlayerInfo> playersList, out List<PlayerInformation> gamePlayers, out Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            gamePlayers = new List<PlayerInformation>();
            gameCallbacks = new Dictionary<int, ITrucoCallback>();

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    foreach (var pInfo in playersList)
                    {
                        if (pInfo.Username.StartsWith(GUEST_PREFIX))
                        {
                            ProcessGuestPlayer(pInfo, gamePlayers, gameCallbacks);
                        }
                        else
                        {
                            ProcessRegisteredPlayer(context, pInfo, gamePlayers, gameCallbacks);
                        }
                    }
                }

                if (gamePlayers.Count != gameCallbacks.Count)
                {
                    LogManager.LogError(new Exception($"Discrepancy: {gamePlayers.Count} players vs {gameCallbacks.Count} connections."), nameof(BuildGamePlayersAndCallbacks));
                    return false;
                }

                return true;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(BuildGamePlayersAndCallbacks)} - SQL Server Error");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(BuildGamePlayersAndCallbacks)} - Invalid Operation (DataBase Context)");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(BuildGamePlayersAndCallbacks)} - Callback Communication Error");
                return false;
            }
            catch (TimeoutException ex)
            {
                LogManager.LogError(ex, $"{nameof(BuildGamePlayersAndCallbacks)} - Timeout Error");
                return false;
            }
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, $"{nameof(BuildGamePlayersAndCallbacks)} - Null Argument");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(BuildGamePlayersAndCallbacks));
                return false;
            }
        }

        private static void ProcessGuestPlayer(PlayerInfo pInfo, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            int guestTempId = -Math.Abs(pInfo.Username.GetHashCode());

            gamePlayers.Add(new PlayerInformation(guestTempId, pInfo.Username, pInfo.Team));

            var guestCb = matchCallbackToPlayer.FirstOrDefault(kvp => kvp.Value.Username == pInfo.Username).Key;

            if (guestCb != null)
            {
                gameCallbacks[guestTempId] = guestCb;
            }
        }

        private static void ProcessRegisteredPlayer(baseDatosTrucoEntities context, PlayerInfo pInfo, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            var user = context.User.FirstOrDefault(u => u.username == pInfo.Username);

            if (user != null)
            {
                gamePlayers.Add(new PlayerInformation(user.userID, user.username, pInfo.Team));

                var userCallbacks = matchCallbackToPlayer
                    .Where(kvp => kvp.Value.Username == pInfo.Username)
                    .Select(kvp => kvp.Key)
                    .ToList();

                ITrucoCallback activeCallback = null;

                foreach (var cb in userCallbacks)
                {
                    if (((ICommunicationObject)cb).State == CommunicationState.Opened)
                    {
                        activeCallback = cb;
                    }
                    else
                    {
                        matchCallbackToPlayer.TryRemove(cb, out _);
                    }
                }

                if (activeCallback != null)
                {
                    gameCallbacks[user.userID] = activeCallback;
                }
                else
                {
                    Console.WriteLine($"[WARNING] User {pInfo.Username} is in the lobby but has no active WCF connection.");
                }
            }
        }

        private void NotifyMatchStart(string matchCode, List<PlayerInfo> players)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnMatchStarted(matchCode, players);
                }
                catch (CommunicationException ex)
                {
                    LogManager.LogError(ex, $"{nameof(NotifyMatchStart)} - Callback Communication Error");
                }
                catch (TimeoutException ex)
                {
                    LogManager.LogError(ex, $"{nameof(NotifyMatchStart)} - Timeout Error");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex, nameof(NotifyMatchStart));
                }
            });

            Console.WriteLine($"[SERVER] Match {matchCode} started by the owner.");
        }

        private void HandleMatchStartupCleanup(string matchCode)
        {
            if (!matchCodeToLobbyId.TryGetValue(matchCode, out int lobbyId))
            {
                Console.WriteLine($"[WARNING] No lobby found for {matchCode} during match start cleanup.");
                return;
            }

            try
            {
                bool closedLobby = CloseLobbyById(lobbyId);
                bool expiredInvitation = ExpireInvitationByMatchCode(matchCode);

                Task.Delay(300).Wait();
                bool removedLobby = RemoveLobbyMembersById(lobbyId);

                if (!closedLobby || !expiredInvitation || !removedLobby)
                {
                    Console.WriteLine($"[WARNING] Partial cleanup for {matchCode} (closed={closedLobby}, expired={expiredInvitation}, removed={removedLobby}).");
                }
                else
                {
                    Console.WriteLine($"[SERVER] Lobby {matchCode} fully cleaned after match start.");
                }

                matchCodeToLobbyId.TryRemove(matchCode, out _);
                lobbyLocks.TryRemove(lobbyId, out _);
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(HandleMatchStartupCleanup)} - Callback Communication Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(HandleMatchStartupCleanup));
            }
        }

        private bool SwitchGuestTeam(string matchCode, string username)
        {
            PlayerInfo guestInfo = null;

            if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
            {
                guestInfo = callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                                     .FirstOrDefault(info => info != null && info.Username == username);
            }

            if (guestInfo == null)
            {
                LogManager.LogWarn($"[TEAM SWITCH]: Guest {username} not found in callbacks", nameof(SwitchTeam));
                return false;
            }

            string currentTeam = guestInfo.Team;
            string newTeam = (currentTeam == TEAM_1) ? TEAM_2 : TEAM_1;

            if (!CanJoinTeam(matchCode, newTeam))
            {
                Console.WriteLine($"[TEAM SWITCH] Denied: Guest {username} tried join to {newTeam} (full) in {matchCode}.");
                return false;
            }

            guestInfo.Team = newTeam;

            Console.WriteLine($"[TEAM SWITCH] Guest {username} changed to {newTeam} in {matchCode}.");
            return true;
        }

        private bool SwitchUserTeam(string matchCode, string username)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = FindLobbyByMatchCode(context, matchCode, true);

                if (lobby == null)
                {
                    LogManager.LogWarn($"[TEAM SWITCH]: Lobby not found {matchCode}", nameof(SwitchTeam));
                    return false;
                }

                if (lobby.maxPlayers == 2)
                {
                    Console.WriteLine($"[TEAM SWITCH] Ignored for 1v1 match {matchCode} (no team balance needed).");
                    return false;
                }

                var user = context.User.FirstOrDefault(u => u.username == username);

                if (user == null)
                {
                    return false;
                }

                var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobby.lobbyID && lm.userID == user.userID);

                if (member == null)
                {
                    return false;
                }

                string newTeam = (member.team == TEAM_1) ? TEAM_2 : TEAM_1;

                if (!CanJoinTeam(matchCode, newTeam))
                {
                    Console.WriteLine($"[TEAM SWITCH] Denied: {username} attempted to join {newTeam} (full) in {matchCode}");
                    return false;
                }

                member.team = newTeam;
                context.SaveChanges();
                Console.WriteLine($"[TEAM SWITCH] {username} switched to {newTeam} at {matchCode}.");

                return true;
            }
        }

        private bool CanJoinTeam(string matchCode, string targetTeam)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = FindLobbyByMatchCode(context, matchCode, true);

                if (lobby == null)
                {
                    return false;
                }

                int maxPerTeam = lobby.maxPlayers / 2;
                int dbCount = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == targetTeam);
                int memCount = 0;

                if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                {
                    memCount = callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                                        .Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX) && info.Team == targetTeam);
                }

                return (dbCount + memCount) < maxPerTeam;
            }
        }

        private bool GetMatchAndPlayerID(string matchCode, out TrucoMatch match, out int playerID)
        {
            match = null;
            playerID = -1;

            if (!runningGames.TryGetValue(matchCode, out match))
            {
                LogManager.LogWarn($"Method call on non-existent running game: {matchCode}", nameof(GetMatchAndPlayerID));
                return false;
            }

            var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

            if (callback == null)
            {
                LogManager.LogWarn($"Method call with no callback context: {matchCode}", nameof(GetMatchAndPlayerID));
                return false;
            }

            if (!matchCallbackToPlayer.TryGetValue(callback, out PlayerInfo playerInfo))
            {
                LogManager.LogWarn($"Method call from unknown callback: {matchCode}", nameof(GetMatchAndPlayerID));
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.username == playerInfo.Username); 
                    
                    if (user == null)
                    {
                        LogManager.LogWarn($"Callback {playerInfo.Username} not found in DB: {matchCode}", nameof(GetMatchAndPlayerID));
                        return false;
                    }
                    playerID = user.userID; 
                    
                    return true;
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, "GetMatchAndPlayerID (DataBase Query)");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, "GetMatchAndPlayerID (WCF Context or DataBase Operation)");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, "GetMatchAndPlayerID (WCF Channel)");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, "GetMatchAndPlayerID (General)");
                return false;
            }
        }

        private PlayerInfo CreatePlayerInfoForChat(string matchCode, string player)
        {
            if (player.StartsWith(GUEST_PREFIX))
            {
                string assignedTeam = TEAM_1; using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = FindLobbyByMatchCode(context, matchCode, true); 
                    
                    if (lobby != null && lobby.maxPlayers > 2)
                    {
                        int team1DbCount = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_1);
                        int team2DbCount = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == TEAM_2); 

                        int team1MemCount = matchCallbacks[matchCode]
                            .Select(cb => GetPlayerInfoFromCallback(cb))
                            .Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX) && info.Team == TEAM_1); 
                        
                        int team2MemCount = matchCallbacks[matchCode]
                            .Select(cb => GetPlayerInfoFromCallback(cb))
                            .Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX) && info.Team == TEAM_2); 
                        
                        if ((team1DbCount + team1MemCount) >= (team2DbCount + team2MemCount))
                        {
                            assignedTeam = TEAM_2;
                        }
                    }
                }
                return new PlayerInfo { Username = player, Team = assignedTeam, AvatarId = DEFAULT_AVATAR_ID };
            }
            else
            {
                return new PlayerInfo { Username = player };
            }
        }

        private void RemoveInactiveCallbacks(string matchCode)
        {
            if (string.IsNullOrEmpty(matchCode))
            {
                return;
            }

            try
            {
                lock (matchCallbacks)
                {
                    if (!matchCallbacks.TryGetValue(matchCode, out var list))
                    {
                        return;
                    }

                    list.RemoveAll(cb =>
                    {
                        try
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
                        }
                        catch (Exception)
                        {
                            return true;
                        }

                        return false;
                    });
                }
            }
            catch (SynchronizationLockException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveInactiveCallbacks)} - Synchronization Block Error");
            }
            catch (OutOfMemoryException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveInactiveCallbacks)} - Insufficient Memory For Operation");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RemoveInactiveCallbacks));
            }
        }

        private void BroadcastToMatchCallbacksAsync(string matchCode, Action<ITrucoCallback> invocation)
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
                    Task.Run(() =>
                    {
                        ProcessSingleCallbackAsync(matchCode, cb, invocation);
                    });
                }
            }
            catch (OutOfMemoryException ex)
            {
                LogManager.LogError(ex, $"{nameof(BroadcastToMatchCallbacksAsync)} - Insufficient Memory For Snapshot");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(BroadcastToMatchCallbacksAsync));
            }
        }

        private void ProcessSingleCallbackAsync(string matchCode, ITrucoCallback cb, Action<ITrucoCallback> invocation)
        {
            try
            {
                var comm = (ICommunicationObject)cb; 
                
                if (comm.State != CommunicationState.Opened)
                {
                    lock (matchCallbacks)
                    {
                        RemoveInactiveCallbacks(matchCode);
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
            catch (SynchronizationLockException ex)
            {
                LogManager.LogError(ex, $"{nameof(ProcessSingleCallbackAsync)} - Synchronization Block Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ProcessSingleCallbackAsync));
            }
        }

        private static string GenerateMatchCode()
        {
            const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; 
            
            char[] result = new char[MATCH_CODE_LENGTH]; 
            
            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] randomBytes = new byte[result.Length];
                    rng.GetBytes(randomBytes); 
                    
                    for (int i = 0; i < result.Length; i++)
                    {
                        int index = randomBytes[i] % CHARS.Length;
                        result[i] = CHARS[index];
                    }
                }
                return new string(result);
            }
            catch (SynchronizationLockException ex)
            {
                LogManager.LogError(ex, $"{nameof(GenerateMatchCode)} - Synchronization Block Error");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GenerateMatchCode));
                return string.Empty;
            }
        }

        private static int GenerateNumericCodeFromString(string code)
        {
            unchecked
            {
                int hash = 17;

                foreach (char c in code)
                {
                    hash = hash * 31 + c;
                }

                return Math.Abs(hash % 999999);
            }
        }

        private Lobby FindLobbyByMatchCode(baseDatosTrucoEntities context, string matchCode, bool onlyOpen = true)
        {
            try
            {
                int numericCode = GenerateNumericCodeFromString(matchCode); Lobby lobby = GetLobbyByMapping(context, matchCode, onlyOpen);

                if (lobby != null)
                {
                    return lobby;
                }

                var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);

                if (invitation == null)
                {
                    return null;
                }

                return GetLobbyByOwner(context, invitation.senderID, onlyOpen);
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(FindLobbyByMatchCode)} - SQL Server Error");
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(FindLobbyByMatchCode)} - Invalid Operation (Database Context/Mapping)");
            }
            catch (FormatException ex)
            {
                LogManager.LogError(ex, $"{nameof(FindLobbyByMatchCode)} - Code Format Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(FindLobbyByMatchCode));
            }
            return null;
        }

        private Lobby GetLobbyByMapping(baseDatosTrucoEntities context, string matchCode, bool onlyOpen)
        {
            if (!matchCodeToLobbyId.TryGetValue(matchCode, out int mappedLobbyId))
            {
                return null;
            }

            IQueryable<Lobby> query = context.Lobby.Where(l => l.lobbyID == mappedLobbyId); 
            
            if (onlyOpen)
            {
                query = query.Where(l => l.status == PUBLIC_STATUS || l.status == PRIVATE_STATUS);
            }

            return query.FirstOrDefault();
        }

        private static Lobby GetLobbyByOwner(baseDatosTrucoEntities context, int ownerId, bool onlyOpen)
        {
            IQueryable<Lobby> query = context.Lobby.Where(l => l.ownerID == ownerId); 
            
            if (onlyOpen)
            {
                query = query.Where(l => l.status == PUBLIC_STATUS || l.status == PRIVATE_STATUS);
            }

            return query.FirstOrDefault();
        }

        private static string GetLobbyOwnerName(baseDatosTrucoEntities context, int ownerId)
        {
            return context.User
                .Where(u => u.userID == ownerId)
                .Select(u => u.username)
                .FirstOrDefault();
        }

        private List<PlayerInfo> GetDatabasePlayers(baseDatosTrucoEntities context, Lobby lobby, string ownerUsername)
        {
            return (from lm in context.LobbyMember
                    join u in context.User on lm.userID equals u.userID
                    join up in context.UserProfile on u.userID equals up.userID into upj
                    from up in upj.DefaultIfEmpty()
                    where lm.lobbyID == lobby.lobbyID
                    select new PlayerInfo
                    {
                        Username = u.username,
                        AvatarId = up != null ? up.avatarID : DEFAULT_AVATAR_ID,
                        OwnerUsername = ownerUsername,
                        Team = lm.team
                    }).ToList();
        }

        private List<PlayerInfo> GetGuestPlayersFromMemory(string matchCode, string ownerUsername = null)
        {
            if (!matchCallbacks.TryGetValue(matchCode, out var callbacks))
            {
                return new List<PlayerInfo>();
            }

            return callbacks
                .Select(cb => GetPlayerInfoFromCallback(cb))
                .Where(info => info != null && info.Username.StartsWith(GUEST_PREFIX))
                .Select(g => new PlayerInfo
                {
                    Username = g.Username,
                    AvatarId = g.AvatarId ?? DEFAULT_AVATAR_ID,
                    OwnerUsername = ownerUsername,
                    Team = g.Team ?? TEAM_1
                })
                .ToList();
        }

        private static void MergePlayersLists(List<PlayerInfo> mainList, List<PlayerInfo> guests)
        {
            mainList.AddRange(guests.Where(g => !mainList.Any(p => p.Username == g.Username)));
        }

        private static PlayerInfo GetPlayerInfoFromCallback(ITrucoCallback callback)
        {
            try
            {
                if (matchCallbackToPlayer.TryGetValue(callback, out PlayerInfo info))
                {
                    return info;
                }

                return null;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPlayerInfoFromCallback)} - Callback Communication Error");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPlayerInfoFromCallback)} - Error retrieving player info from callback");
                return null;
            }
        }

        private static bool CloseLobbyById(int lobbyId)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == lobbyId);

                    if (lobby == null)
                    {
                        return false;
                    }

                    if (lobby.status != CLOSED_STATUS)
                    {
                        lobby.status = CLOSED_STATUS;
                        context.SaveChanges();
                    }

                    return true;
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                LogManager.LogError(ex, $"{nameof(CloseLobbyById)} - Concurrency");
                return false;
            }
            catch (UpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(CloseLobbyById)} - Update Error");
                return false;
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, $"{nameof(CloseLobbyById)} - Entity Validation");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(CloseLobbyById)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(CloseLobbyById)} - SQL Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CloseLobbyById));
                return false;
            }
        }

        private static bool ExpireInvitationByMatchCode(string matchCode)
        {
            try
            {
                int numericCode = GenerateNumericCodeFromString(matchCode);

                using (var context = new baseDatosTrucoEntities())
                {
                    var invitations = context.Invitation
                        .Where(i => i.code == numericCode && i.status == PENDING_STATUS)
                        .ToList(); 
                    
                    if (!invitations.Any())
                    {
                        return true;
                    }

                    foreach (var inv in invitations)
                    {
                        inv.status = EXPIRED_STATUS;
                        inv.expiresAt = DateTime.Now;
                    }

                    context.SaveChanges();
                    return true;
                }
            }
            catch (FormatException ex)
            {
                LogManager.LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - Invalid Code");
                return false;
            }
            catch (OverflowException ex)
            {
                LogManager.LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - Code Out of Range");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - SQL Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ExpireInvitationByMatchCode));
                return false;
            }
        }
        private static bool RemoveLobbyMembersById(int lobbyId)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var members = context.LobbyMember.Where(lm => lm.lobbyID == lobbyId).ToList();

                    if (!members.Any())
                    {
                        return true;
                    }

                    context.LobbyMember.RemoveRange(members);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (UpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 547))
            {
                LogManager.LogError(ex, $"{nameof(RemoveLobbyMembersById)} - FK Restriction (Code 547)");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveLobbyMembersById)} - Database Deleting Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveLobbyMembersById)} - SQL Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RemoveLobbyMembersById));
                return false;
            }
        }
    }
}
