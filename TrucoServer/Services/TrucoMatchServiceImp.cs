using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Utilities;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class TrucoMatchServiceImp : ITrucoMatchService
    {
        private const string STATUS_PUBLIC = "Public";
        private const string STATUS_PRIVATE = "Private";
        private const string STATUS_CLOSED = "Closed";
        private const string GUEST_PREFIX = "Guest_";

        private readonly IGameRegistry gameRegistry;
        private readonly IJoinService joinService;
        private readonly ILobbyCoordinator coordinator;
        private readonly ILobbyRepository repository;
        private readonly IMatchCodeGenerator codeGenerator;
        private readonly IMatchStarter starter;

        public TrucoMatchServiceImp()
        {
            var coordinator = new LobbyCoordinator();
            var registry = new GameRegistry();
            var repository = new LobbyRepository();
            var generator = new MatchCodeGenerator();

            var gameManager = new TrucoGameManager();
            var shuffler = new DefaultDeckShuffler();


            var joinService = new JoinService(coordinator, repository);

            var starter = new MatchStarter(
                registry,
                coordinator,
                repository,
                shuffler,
                gameManager
            );

            this.gameRegistry = registry;
            this.joinService = joinService;
            this.coordinator = coordinator;
            this.repository = repository;
            this.codeGenerator = generator;
            this.starter = starter;
        }

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

                    int versionId = repository.ResolveVersionId(context, maxPlayers);
                    string matchCode = codeGenerator.GenerateMatchCode();
                    string normalizedStatus = privacy.Equals("public", StringComparison.OrdinalIgnoreCase) ? STATUS_PUBLIC : STATUS_PRIVATE;

                    var lobby = repository.CreateNewLobby(context, host, versionId, maxPlayers, normalizedStatus);

                    if (lobby == null)
                    {
                        return string.Empty;
                    }

                    repository.AddLobbyOwner(context, lobby, host);

                    if (lobby.status.Equals(STATUS_PRIVATE, StringComparison.OrdinalIgnoreCase))
                    {
                        repository.CreatePrivateInvitation(context, host, matchCode);
                    }

                    context.SaveChanges();

                    coordinator.RegisterLobbyMapping(matchCode, lobby);
                    coordinator.GetOrCreateLobbyLock(lobby.lobbyID);

                    Console.WriteLine($"[SERVER] Lobby created by {hostUsername}, code={matchCode}, privacy={privacy}, maxPlayers={maxPlayers}.");
                    return matchCode;
                }
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
            int lobbyId = 0;

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = null;
                    if (coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        lobby = repository.ResolveLobbyForJoin(context, matchCode);
                    }

                    if (lobby == null || lobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[JOIN] Denied: Lobby closed/not found for code {matchCode}.");
                        return false;
                    }
                    lobbyId = lobby.lobbyID;
                }

                object lobbyLock = coordinator.GetOrCreateLobbyLock(lobbyId);
                lock (lobbyLock)
                {
                    joinSuccess = joinService.ProcessSafeJoin(lobbyId, matchCode, player);
                }

                if (joinSuccess)
                {
                    Console.WriteLine($"[SERVER] {player} joined lobby {matchCode}.");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(JoinMatch));
            }

            return joinSuccess;
        }

        public void LeaveMatch(string matchCode, string player)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(player)) return;

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = null;
                    if (coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        lobby = repository.ResolveLobbyForLeave(context, matchCode, player, out _);
                    }

                    var user = context.User.FirstOrDefault(u => u.username == player);

                    if (lobby == null || user == null)
                    {
                        return;
                    }

                    var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobby.lobbyID && lm.userID == user.userID);
                    if (member != null)
                    {
                        context.LobbyMember.Remove(member);
                        context.SaveChanges();
                        Console.WriteLine($"[LEAVE] Player '{player}' removed from lobby {lobby.lobbyID}.");
                    }

                    coordinator.NotifyPlayerLeft(matchCode, player);

                    if (!context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID))
                    {
                        repository.CloseLobbyById(lobby.lobbyID);
                        repository.ExpireInvitationByMatchCode(matchCode);
                        repository.RemoveLobbyMembersById(lobby.lobbyID);
                        starter.HandleMatchStartupCleanup(matchCode);
                        Console.WriteLine($"[CLEANUP] Lobby {lobby.lobbyID} closed.");
                    }
                }
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

                        string matchCode = coordinator.GetMatchCodeFromLobbyId(lobby.lobbyID) ?? "(unknown)"; ;

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
                    Lobby lobby = null;
                    if (coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        lobby = repository.FindLobbyByMatchCode(context, matchCode, false);
                    }

                    if (lobby == null)
                    {
                        return coordinator.GetGuestPlayersFromMemory(matchCode);
                    }

                    string ownerUsername = repository.GetLobbyOwnerName(context, lobby.ownerID);
                    var dbPlayers = repository.GetDatabasePlayers(context, lobby, ownerUsername);
                    var guestPlayers = coordinator.GetGuestPlayersFromMemory(matchCode, ownerUsername);

                    dbPlayers.AddRange(guestPlayers.Where(g => !dbPlayers.Any(p => p.Username == g.Username)));
                    return dbPlayers;
                }
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
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = null;
                    if (coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        lobby = repository.FindLobbyByMatchCode(context, matchCode, true);
                    }

                    if (lobby == null)
                    {
                        return;
                    }

                    var dbMembers = context.LobbyMember.Where(lm => lm.lobbyID == lobby.lobbyID).ToList();
                    int guestCount = coordinator.GetGuestCountInMemory(matchCode);

                    if ((dbMembers.Count + guestCount) != lobby.maxPlayers)
                    {
                        return;
                    }
                }

                List<PlayerInfo> playersList = GetLobbyPlayers(matchCode);

                if (!starter.BuildGamePlayersAndCallbacks(playersList, out var gamePlayers, out var gameCallbacks))
                {
                    return;
                }

                int lobbyId;

                if (!coordinator.TryGetLobbyIdFromCode(matchCode, out lobbyId))
                {
                    using (var ctx = new baseDatosTrucoEntities())
                    {
                        var l = repository.FindLobbyByMatchCode(ctx, matchCode, false);
                        lobbyId = l?.lobbyID ?? 0;
                    }
                }

                starter.InitializeAndRegisterGame(matchCode, lobbyId, gamePlayers, gameCallbacks);
                starter.NotifyMatchStart(matchCode, playersList);
                starter.HandleMatchStartupCleanup(matchCode);

                Task.Delay(500).ContinueWith(_ =>
                {
                    if (gameRegistry.TryGetGame(matchCode, out var match))
                    {
                        match.StartNewHand();
                    }
                });
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
                coordinator.RemoveInactiveCallbacks(matchCode);

                bool isNew = coordinator.RegisterChatCallback(matchCode, player, callback);
                Console.WriteLine($"[CHAT] {player} joined lobby {matchCode}.");

                if (isNew)
                {
                    coordinator.NotifyPlayerJoined(matchCode, player);
                }
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
                coordinator.RemoveCallbackFromMatch(matchCode, callback);
                coordinator.NotifyPlayerLeft(matchCode, player);
                Console.WriteLine($"[CHAT] {player} left lobby {matchCode}.");

                gameRegistry.AbortAndRemoveGame(matchCode, player);
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
                coordinator.RemoveInactiveCallbacks(matchCode);
                var senderCallback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
                {
                    if (!ReferenceEquals(cb, senderCallback))
                    {
                        try 
                        { 
                            cb.OnChatMessage(matchCode, player, message); 
                        }
                        catch (Exception ex)
                        { 
                            LogManager.LogError(ex, "Chat send error"); 
                        }
                    }
                });
                Console.WriteLine($"[{matchCode}] {player}: {message}");
            }
            catch (Exception ex) 
            { 
                LogManager.LogError(ex, nameof(SendChatMessage)); 
            }
        }

        public void PlayCard(string matchCode, string cardFileName) => ExecuteGameAction(matchCode, (m, pid) => m.PlayCard(pid, cardFileName), nameof(PlayCard));
        public void CallTruco(string matchCode, string betType) => ExecuteGameAction(matchCode, (m, pid) => m.CallTruco(pid, betType), nameof(CallTruco));
        public void RespondToCall(string matchCode, string response) => ExecuteGameAction(matchCode, (m, pid) => m.RespondToCall(pid, response), nameof(RespondToCall));
        public void CallEnvido(string matchCode, string betType) => ExecuteGameAction(matchCode, (m, pid) => m.CallEnvido(pid, betType), nameof(CallEnvido));
        public void RespondToEnvido(string matchCode, string response) => ExecuteGameAction(matchCode, (m, pid) => m.RespondToEnvido(pid, response), nameof(RespondToEnvido));
        public void CallFlor(string matchCode, string betType) => ExecuteGameAction(matchCode, (m, pid) => m.CallFlor(pid, betType), nameof(CallFlor));
        public void RespondToFlor(string matchCode, string response) => ExecuteGameAction(matchCode, (m, pid) => m.RespondToFlor(pid, response), nameof(RespondToFlor));
        public void GoToDeck(string matchCode) => ExecuteGameAction(matchCode, (m, pid) => m.PlayerGoesToDeck(pid), nameof(GoToDeck));

        public void SwitchTeam(string matchCode, string username)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(username))
            {
                return;
            }
            try
            {
                bool success = username.StartsWith(GUEST_PREFIX)
                    ? joinService.SwitchGuestTeam(matchCode, username)
                    : joinService.SwitchUserTeam(matchCode, username);

                if (success)
                {
                    coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb => cb.OnPlayerJoined(matchCode, username));
                }
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
                if (starter.GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    action(match, playerID);
                }
            }
            catch (Exception ex) 
            { 
                LogManager.LogError(ex, callerName);
            }
        }
    }
}
