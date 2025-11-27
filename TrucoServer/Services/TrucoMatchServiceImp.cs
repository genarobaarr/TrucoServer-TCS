using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
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
        private const string STATUS_EXPIRED = "Expired";
        private const string STATUS_PENDING = "Pending";
        private const string STATUS_PUBLIC = "Public";
        private const string STATUS_PRIVATE = "Private";
        private const string STATUS_CLOSED = "Closed";
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
            if (!ServerValidator.IsUsernameValid(hostUsername))
            {
                return string.Empty;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var host = context.User.FirstOrDefault(u => u.username == hostUsername);

                    if (host == null)
                    {
                        throw new InvalidOperationException("Host user not found.");
                    }

                    int versionId = ResolveVersionId(context, maxPlayers);
                    string matchCode = GenerateMatchCode();
                    string normalizedStatus = privacy.Equals("public", StringComparison.OrdinalIgnoreCase) ? STATUS_PUBLIC : STATUS_PRIVATE;

                    var lobby = CreateNewLobby(context, host, versionId, maxPlayers, normalizedStatus);

                    if (lobby == null)
                    {
                        return string.Empty;
                    }

                    AddLobbyOwner(context, lobby, host);

                    if (lobby.status.Equals(STATUS_PRIVATE, StringComparison.OrdinalIgnoreCase))
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
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(CreateLobby)} - Logic Error");
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
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(player))
            {
                return false;
            }

            bool joinSuccess = false;
            Lobby lobby = null;

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    lobby = ResolveLobbyForJoin(context, matchCode);

                    if (lobby == null || lobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
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
                    Console.WriteLine($"[SERVER] {player} joined lobby {matchCode}.");
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
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(player))
            {
                return;
            }

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

        public List<PublicLobbyInfo> GetPublicLobbies()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var publicLobbies = context.Lobby
                        .Where(l => l.status == STATUS_PUBLIC)
                        .Select(l => new { l.lobbyID, l.maxPlayers, l.createdAt, l.ownerID })
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPublicLobbies));
                return new List<PublicLobbyInfo>();
            }
        }

        public List<PlayerInfo> GetLobbyPlayers(string matchCode)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode))
            {
                return new List<PlayerInfo>();
            }

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
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLobbyPlayers)} - SQL Server Error");
                return new List<PlayerInfo>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetLobbyPlayers));
                return new List<PlayerInfo>();
            }
        }

        public void StartMatch(string matchCode)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode))
            {
                return;
            }

            try
            {
                ValidatedLobbyData validatedData;
                using (var context = new baseDatosTrucoEntities())
                {
                    validatedData = GetAndValidateLobbyForStart(context, matchCode);
                    if (validatedData == null)
                    {
                        return;
                    }
                }

                List<PlayerInfo> playersList = GetLobbyPlayers(matchCode);

                if (!BuildGamePlayersAndCallbacks(playersList, out var gamePlayers, out var gameCallbacks))
                {
                    return;
                }

                InitializeAndRegisterGame(matchCode, validatedData.Lobby.lobbyID, gamePlayers, gameCallbacks);

                NotifyMatchStart(matchCode, playersList);
                HandleMatchStartupCleanup(matchCode);

                Task.Delay(500).ContinueWith(_ => runningGames[matchCode].StartNewHand());
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

        public void JoinMatchChat(string matchCode, string player)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(player))
            {
                return;
            }

            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                RemoveInactiveCallbacks(matchCode);

                bool isNewCallback = RegisterChatCallback(matchCode, player, callback);

                Console.WriteLine($"[CHAT] {player} joined the lobby {matchCode}.");

                if (isNewCallback)
                {
                    NotifyPlayerJoined(matchCode, player);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(JoinMatchChat)} - No WCF Context");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(JoinMatchChat));
            }
        }

        public void LeaveMatchChat(string matchCode, string player)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(player))
            {
                return;
            }

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
                    NotifyPlayerLeftChat(matchCode, player);
                }

                Console.WriteLine($"[CHAT] {player} left the lobby {matchCode}.");
                TerminateRunningGameIfExist(matchCode, player);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(LeaveMatchChat));
            }
        }

        public void SendChatMessage(string matchCode, string player, string message)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

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
                    catch (CommunicationException ex) { LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Client disconnected"); }
                    catch (TimeoutException ex) { LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Timeout"); }
                    catch (Exception ex) { LogManager.LogError(ex, $"{nameof(SendChatMessage)} - Error"); }
                });

                Console.WriteLine($"[{matchCode}] {player}: {message}");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SendChatMessage));
            }
        }

        public void PlayCard(string matchCode, string cardFileName)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.PlayCard(pid, cardFileName), nameof(PlayCard));
        }

        public void CallTruco(string matchCode, string betType)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.CallTruco(pid, betType), nameof(CallTruco));
        }

        public void RespondToCall(string matchCode, string response)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.RespondToCall(pid, response), nameof(RespondToCall));
        }

        public void CallEnvido(string matchCode, string betType)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.CallEnvido(pid, betType), nameof(CallEnvido));
        }

        public void RespondToEnvido(string matchCode, string response)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.RespondToEnvido(pid, response), nameof(RespondToEnvido));
        }

        public void CallFlor(string matchCode, string betType)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.CallFlor(pid, betType), nameof(CallFlor));
        }

        public void RespondToFlor(string matchCode, string response)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.RespondToFlor(pid, response), nameof(RespondToFlor));
        }

        public void GoToDeck(string matchCode)
        {
            ExecuteGameAction(matchCode, (m, pid) => m.PlayerGoesToDeck(pid), nameof(GoToDeck));
        }

        public void SwitchTeam(string matchCode, string username)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(username))
            {
                return;
            }

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

        private void ExecuteGameAction(string matchCode, Action<TrucoMatch, int> action, string callerName)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode))
            {
                return;
            }

            try
            {
                if (GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    action(match, playerID);
                }
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{callerName} - Invalid Operation");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, callerName);
            }
        }

        private void InitializeAndRegisterGame(string matchCode, int lobbyId, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            var newDeck = new Deck(shuffler);
            var newGame = new TrucoMatch(matchCode, lobbyId, gamePlayers, gameCallbacks, newDeck, gameManager);

            if (!runningGames.TryAdd(matchCode, newGame))
            {
                LogManager.LogError(new Exception($"Failed to add running game {matchCode}"), nameof(InitializeAndRegisterGame));
            }
        }

        private bool RegisterChatCallback(string matchCode, string player, ITrucoCallback callback)
        {
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
                    return true;
                }
                return false;
            }
        }

        private void NotifyPlayerLeftChat(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnPlayerLeft(matchCode, player);
                }
                catch (CommunicationException ex) { LogManager.LogError(ex, $"{nameof(NotifyPlayerLeftChat)} - Comm Error"); }
                catch (TimeoutException ex) { LogManager.LogError(ex, $"{nameof(NotifyPlayerLeftChat)} - Timeout"); }
                catch (Exception ex) { LogManager.LogError(ex, nameof(NotifyPlayerLeftChat)); }
            });
        }

        private void TerminateRunningGameIfExist(string matchCode, string player)
        {
            if (runningGames.TryGetValue(matchCode, out TrucoMatch match))
            {
                match.AbortMatch(player);
                runningGames.TryRemove(matchCode, out _);
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
                    .Where(i => i.senderID == host.userID && i.status == STATUS_PENDING)
                    .ToList();

                foreach (var inv in previousInvitations)
                {
                    inv.status = STATUS_EXPIRED;
                    inv.expiresAt = DateTime.Now;
                }

                context.Invitation.Add(new Invitation
                {
                    senderID = host.userID,
                    receiverEmail = null,
                    code = numericCode,
                    status = STATUS_PENDING,
                    expiresAt = DateTime.Now.AddHours(2)
                });
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
                    if (lobby != null && !lobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
                    {
                        return lobby;
                    }
                    matchCodeToLobbyId.TryRemove(matchCode, out _);
                    return null;
                }

                int numericCode = GenerateNumericCodeFromString(matchCode);
                var invitation = context.Invitation.FirstOrDefault(i =>
                    i.code == numericCode && i.status == STATUS_PENDING && i.expiresAt > DateTime.Now);

                if (invitation == null)
                {
                    return null;
                }

                var lobbyCandidate = context.Lobby.FirstOrDefault(l =>
                    l.ownerID == invitation.senderID && !l.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase));

                if (lobbyCandidate != null)
                {
                    matchCodeToLobbyId.TryAdd(matchCode, lobbyCandidate.lobbyID);
                }

                return lobbyCandidate;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(ResolveLobbyForJoin)} - SQL Error");
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
                if (freshLobby == null || freshLobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[JOIN] Denied: Lobby closed while waiting for lock.");
                    return false;
                }

                bool isGuest = player.StartsWith(GUEST_PREFIX);
                return isGuest ? TryJoinAsGuest(context, freshLobby, matchCode, player) : TryJoinAsUser(context, freshLobby, player);
            }
        }

        private bool TryJoinAsGuest(baseDatosTrucoEntities context, Lobby lobby, string matchCode, string player)
        {
            if (!lobby.status.Equals(STATUS_PUBLIC, StringComparison.OrdinalIgnoreCase))
            {
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
                return false;
            }
            if (lobby == null || lobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                int count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
                if (count >= lobby.maxPlayers)
                {
                    Console.WriteLine($"[JOIN] Lobby {lobby.lobbyID} is full.");
                    return false;
                }
                return true;
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
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AddPlayerToLobby));
                throw;
            }
        }

        private static string DetermineTeamForNewPlayer(int maxPlayers, int team1Count, int team2Count, string username)
        {
            if (maxPlayers == 2)
            {
                return (team1Count <= team2Count) ? TEAM_1 : TEAM_2;
            }
            return (team1Count > team2Count) ? TEAM_2 : TEAM_1;
        }

        private void NotifyPlayerJoined(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try { cb.OnPlayerJoined(matchCode, player); }
                catch (Exception ex) { LogManager.LogError(ex, nameof(NotifyPlayerJoined)); }
            });
        }

        private Lobby ResolveLobbyForLeave(baseDatosTrucoEntities context, string matchCode, string username, out User player)
        {
            player = null;
            try
            {
                player = context.User.FirstOrDefault(u => u.username == username);
                if (player == null)
                {
                    return null;
                }
                return FindLobbyByMatchCode(context, matchCode, true);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ResolveLobbyForLeave));
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RemovePlayerFromLobby));
                throw;
            }
        }

        private void NotifyPlayerLeft(string matchCode, string username)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try { cb.OnPlayerLeft(matchCode, username); }
                catch (Exception ex) { LogManager.LogError(ex, nameof(NotifyPlayerLeft)); }
            });
        }

        private void HandleEmptyLobbyCleanup(baseDatosTrucoEntities context, Lobby lobby, string matchCode)
        {
            try
            {
                bool isEmpty = !context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID);

                if (isEmpty)
                {
                    CloseLobbyById(lobby.lobbyID);
                    ExpireInvitationByMatchCode(matchCode);
                    RemoveLobbyMembersById(lobby.lobbyID);
                    matchCodeToLobbyId.TryRemove(matchCode, out _);
                    lobbyLocks.TryRemove(lobby.lobbyID, out _);
                    Console.WriteLine($"[CLEANUP] Lobby {lobby.lobbyID} closed.");
                }
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
                    return null;
                }

                var dbMembers = context.LobbyMember.Where(lm => lm.lobbyID == lobby.lobbyID).ToList();
                int guestCount = 0;
                if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                {
                    guestCount = callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                        .Count(info => info != null && info.Username != null && info.Username.StartsWith(GUEST_PREFIX));
                }

                if ((dbMembers.Count + guestCount) != lobby.maxPlayers)
                {
                    return null;
                }

                return new ValidatedLobbyData { Lobby = lobby, Members = dbMembers };
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

                return gamePlayers.Count == gameCallbacks.Count;
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
                    Console.WriteLine($"[WARNING] User {pInfo.Username} is in lobby but has no active connection.");
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
                catch (Exception ex) 
                { 
                    LogManager.LogError(ex, nameof(NotifyMatchStart)); 
                }
            });
            Console.WriteLine($"[SERVER] Match {matchCode} started.");
        }

        private void HandleMatchStartupCleanup(string matchCode)
        {
            if (!matchCodeToLobbyId.TryGetValue(matchCode, out int lobbyId))
            {
                return;
            }
            try
            {
                CloseLobbyById(lobbyId);
                ExpireInvitationByMatchCode(matchCode);
                RemoveLobbyMembersById(lobbyId);
                matchCodeToLobbyId.TryRemove(matchCode, out _);
                lobbyLocks.TryRemove(lobbyId, out _);
            }
            catch (Exception ex) { LogManager.LogError(ex, nameof(HandleMatchStartupCleanup)); }
        }

        private bool SwitchGuestTeam(string matchCode, string username)
        {
            if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
            {
                var guestInfo = callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                    .FirstOrDefault(info => info != null && info.Username == username);

                if (guestInfo != null)
                {
                    string newTeam = (guestInfo.Team == TEAM_1) ? TEAM_2 : TEAM_1;
                    if (CanJoinTeam(matchCode, newTeam))
                    {
                        guestInfo.Team = newTeam;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool SwitchUserTeam(string matchCode, string username)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = FindLobbyByMatchCode(context, matchCode, true);
                if (lobby == null || lobby.maxPlayers == 2)
                {
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
                if (CanJoinTeam(matchCode, newTeam))
                {
                    member.team = newTeam;
                    context.SaveChanges();
                    return true;
                }

                return false;
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

                int dbCount = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == targetTeam);
                int memCount = 0;
                if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                {
                    memCount = callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                        .Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX) && info.Team == targetTeam);
                }

                return (dbCount + memCount) < (lobby.maxPlayers / 2);
            }
        }

        private bool GetMatchAndPlayerID(string matchCode, out TrucoMatch match, out int playerID)
        {
            match = null;
            playerID = -1;
            if (!runningGames.TryGetValue(matchCode, out match))
            {
                return false;
            }

            var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
            if (callback == null || !matchCallbackToPlayer.TryGetValue(callback, out PlayerInfo playerInfo))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.username == playerInfo.Username);
                    if (user == null)
                    {
                        return false;
                    }

                    playerID = user.userID;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetMatchAndPlayerID));
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
                    var lobby = FindLobbyByMatchCode(context, matchCode, true);
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
                return new PlayerInfo { Username = player, Team = assignedTeam, AvatarId = DEFAULT_AVATAR_ID };
            }
            return new PlayerInfo { Username = player };
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
                    if (matchCallbacks.TryGetValue(matchCode, out var list))
                    {
                        list.RemoveAll(cb => {
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
                    Task.Run(() => ProcessSingleCallbackAsync(matchCode, cb, invocation));
                }
            }
            catch (Exception ex) { LogManager.LogError(ex, nameof(BroadcastToMatchCallbacksAsync)); }
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
                        result[i] = CHARS[randomBytes[i] % CHARS.Length];
                    }
                }
                return new string(result);
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
                int numericCode = GenerateNumericCodeFromString(matchCode);
                Lobby lobby = GetLobbyByMapping(context, matchCode, onlyOpen);
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(FindLobbyByMatchCode));
                return null;
            }
        }

        private Lobby GetLobbyByMapping(baseDatosTrucoEntities context, string matchCode, bool onlyOpen)
        {
            if (!matchCodeToLobbyId.TryGetValue(matchCode, out int mappedLobbyId))
            {
                return null;
            }

            var query = context.Lobby.Where(l => l.lobbyID == mappedLobbyId);

            if (onlyOpen)
            {
                query = query.Where(l => l.status == STATUS_PUBLIC || l.status == STATUS_PRIVATE);
            }

            return query.FirstOrDefault();
        }

        private static Lobby GetLobbyByOwner(baseDatosTrucoEntities context, int ownerId, bool onlyOpen)
        {
            var query = context.Lobby.Where(l => l.ownerID == ownerId);

            if (onlyOpen)
            {
                query = query.Where(l => l.status == STATUS_PUBLIC || l.status == STATUS_PRIVATE);
            }

            return query.FirstOrDefault();
        }

        private static string GetLobbyOwnerName(baseDatosTrucoEntities context, int ownerId)
        {
            return context.User.Where(u => u.userID == ownerId).Select(u => u.username).FirstOrDefault();
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

            return callbacks.Select(cb => GetPlayerInfoFromCallback(cb))
                .Where(info => info != null && info.Username.StartsWith(GUEST_PREFIX))
                .Select(g => new PlayerInfo { Username = g.Username, AvatarId = DEFAULT_AVATAR_ID, OwnerUsername = ownerUsername, Team = g.Team ?? TEAM_1 })
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPlayerInfoFromCallback));
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
                    if (lobby.status != STATUS_CLOSED)
                    {
                        lobby.status = STATUS_CLOSED;
                        context.SaveChanges();
                    }
                    return true;
                }
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
                    var invitations = context.Invitation.Where(i => i.code == numericCode && i.status == STATUS_PENDING).ToList();
                    if (!invitations.Any())
                    {
                        return true;
                    }
                    foreach (var inv in invitations)
                    {
                        inv.status = STATUS_EXPIRED;
                        inv.expiresAt = DateTime.Now;
                    }
                    context.SaveChanges();
                    return true;
                }
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RemoveLobbyMembersById));
                return false;
            }
        }
    }
}
