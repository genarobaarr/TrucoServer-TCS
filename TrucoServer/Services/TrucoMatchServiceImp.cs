using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Match;
using TrucoServer.Helpers.Profanity;
using TrucoServer.Helpers.Ranking;
using TrucoServer.Utilities;

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
        private readonly ILobbyCoordinator lobbyCoordinator;
        private readonly ILobbyRepository lobbyRepository;
        private readonly IMatchCodeGenerator codeGenerator;
        private readonly IMatchStarter starter;
        private readonly IProfanityServerService profanityService;

        public TrucoMatchServiceImp()
        {
            var coordinator = new LobbyCoordinator();
            var registry = new GameRegistry();
            var repository = new LobbyRepository();
            var generator = new MatchCodeGenerator();
            var profanity = new BannedWordRepository();
            var statsService = new UserStatsService();
            var gameManager = new TrucoGameManager(statsService);
            var shuffler = new DefaultDeckShuffler();


            var join = new JoinService(coordinator, repository);

            var matchStarter = new MatchStarter(
                registry,
                coordinator,
                repository,
                shuffler,
                gameManager
            );

            this.gameRegistry = registry;
            this.joinService = join;
            this.lobbyCoordinator = coordinator;
            this.lobbyRepository = repository;
            this.codeGenerator = generator;
            this.starter = matchStarter;
            this.profanityService = new ProfanityServerService(profanity);

            this.profanityService.LoadBannedWords();
        }

        public string CreateLobby(string hostUsername, int maxPlayers, string privacy)
        {
            if (!ServerValidator.IsUsernameValid(hostUsername))
            {
                return string.Empty;
            }

            try
            {
                string matchCode = codeGenerator.GenerateMatchCode();

                using (var context = new baseDatosTrucoEntities())
                {
                    var host = context.User.FirstOrDefault(u => u.username == hostUsername);

                    if (host == null)
                    {
                        throw new InvalidOperationException("Host user not found.");
                    }

                    int versionId = lobbyRepository.ResolveVersionId(context, maxPlayers);
                    string normalizedStatus = privacy.Equals("public", StringComparison.OrdinalIgnoreCase) ? STATUS_PUBLIC : STATUS_PRIVATE;

                    var lobbyOptions = new LobbyCreationOptions
                    {
                        Host = host,
                        VersionId = versionId,
                        MaxPlayers = maxPlayers,
                        Status = normalizedStatus
                    };

                    var lobby = lobbyRepository.CreateNewLobby(context, lobbyOptions);

                    if (lobby == null)
                    {
                        return string.Empty;
                    }

                    lobbyRepository.AddLobbyOwner(context, lobby, host);

                    context.SaveChanges();

                    lobbyCoordinator.RegisterLobbyMapping(matchCode, lobby);
                    lobbyCoordinator.GetOrCreateLobbyLock(lobby.lobbyID);

                    if (lobby.status.Equals(STATUS_PRIVATE, StringComparison.OrdinalIgnoreCase))
                    {
                        lobbyRepository.CreatePrivateInvitation(context, host, matchCode);
                        context.SaveChanges();
                    }

                    return matchCode;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CreateLobby));
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
                   
                    if (lobbyCoordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        lobby = lobbyRepository.ResolveLobbyForJoin(context, matchCode);

                        if (lobby != null && !lobby.status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                        {
                            lobbyCoordinator.RegisterLobbyMapping(matchCode, lobby);
                        }
                    }

                    if (lobby == null || lobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    
                    lobbyId = lobby.lobbyID;
                }

                object lobbyLock = lobbyCoordinator.GetOrCreateLobbyLock(lobbyId);

                lock (lobbyLock)
                {
                    joinSuccess = joinService.ProcessSafeJoin(lobbyId, matchCode, player);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatch));
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
                    Lobby lobby = null;
                    User user = null;

                    if (lobbyCoordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        var criteria = new LobbyLeaveCriteria
                        {
                            MatchCode = matchCode,
                            Username = player
                        };

                        var result = lobbyRepository.ResolveLobbyForLeave(context, criteria);

                        if (result != null)
                        {
                            lobby = result.Lobby;
                            user = result.Player;
                        }
                    }

                    if (user == null)
                    {
                        user = context.User.FirstOrDefault(u => u.username == player);
                    }

                    var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobby.lobbyID && lm.userID == user.userID);
                    
                    if (member != null)
                    {
                        context.LobbyMember.Remove(member);
                        context.SaveChanges();
                    }

                    lobbyCoordinator.NotifyPlayerLeft(matchCode, player);

                    if (!context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID))
                    {
                        lobbyRepository.CloseLobbyById(lobby.lobbyID);
                        lobbyRepository.ExpireInvitationByMatchCode(matchCode);
                        lobbyRepository.RemoveLobbyMembersById(lobby.lobbyID);
                        starter.HandleMatchStartupCleanup(matchCode);
                    }
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatch));
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

                        string matchCode = lobbyCoordinator.GetMatchCodeFromLobbyId(lobby.lobbyID) ?? "(unknown)";

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
                ServerException.HandleException(ex, nameof(GetPublicLobbies));
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
                if (gameRegistry.TryGetGame(matchCode, out var match))
                {

                    var gamePlayers = match.Players.Select(p => new PlayerInfo
                    {
                        Username = p.Username,
                        Team = p.Team,
                        AvatarId = starter.GetAvatarIdForPlayer(p.Username),
                        OwnerUsername = starter.GetOwnerUsername(matchCode)
                    }).ToList();

                    return gamePlayers;
                }

                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = null;
                  
                    if (lobbyCoordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                    }

                    if (lobby == null)
                    {
                        lobby = lobbyRepository.FindLobbyByMatchCode(context, matchCode, false);
                    }

                    if (lobby == null)
                    {
                        return lobbyCoordinator.GetGuestPlayersFromMemory(matchCode);
                    }

                    string ownerUsername = lobbyRepository.GetLobbyOwnerName(context, lobby.ownerID);
                    var dbPlayers = lobbyRepository.GetDatabasePlayers(context, lobby, ownerUsername);
                    var guestPlayers = lobbyCoordinator.GetGuestPlayersFromMemory(matchCode, ownerUsername);
                    dbPlayers.AddRange(guestPlayers.Where(g => !dbPlayers.Any(p => p.Username == g.Username)));

                    return dbPlayers;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyPlayers));
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
                var validation = starter.ValidateMatchStart(matchCode);

                if (!validation.IsValid)
                {
                    return;
                }

                List<PlayerInfo> playersList = GetLobbyPlayers(matchCode);

                if (playersList.Count != validation.ExpectedPlayers)
                {
                    return;
                }

                starter.InitiateMatchSequence(matchCode, validation.LobbyId, playersList);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(StartMatch));
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
                lobbyCoordinator.RemoveInactiveCallbacks(matchCode);

                bool isNew = lobbyCoordinator.RegisterChatCallback(matchCode, player, callback);

                if (isNew)
                {

                    Task.Delay(100).ContinueWith(_ =>
                    {
                        lobbyCoordinator.NotifyPlayerJoined(matchCode, player);
                    });
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatchChat));
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

                lobbyCoordinator.RemoveCallbackFromMatch(matchCode, callback);
                lobbyCoordinator.RemoveMatchCallbackMapping(callback);

                lobbyCoordinator.NotifyPlayerLeft(matchCode, player);
                gameRegistry.AbortAndRemoveGame(matchCode, player);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatchChat));
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
                if (profanityService.ContainsProfanity(message))
                {
                    return;
                }

                lobbyCoordinator.RemoveInactiveCallbacks(matchCode);
                var senderCallback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                lobbyCoordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
                {
                    if (!ReferenceEquals(cb, senderCallback))
                    {
                        try 
                        { 
                            cb.OnChatMessage(matchCode, player, message); 
                        }
                        catch (Exception)
                        {
                            // Log and ignore exceptions from individual callbacks
                        }
                    }
                });
            }
            catch (Exception ex) 
            { 
                ServerException.HandleException(ex, nameof(SendChatMessage));
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
                    lobbyCoordinator.BroadcastToMatchCallbacksAsync(matchCode, cb => cb.OnPlayerJoined(matchCode, username));
                }
            }
            catch (Exception ex) 
            { 
                ServerException.HandleException(ex, nameof(SwitchTeam));
            }
        }

        public BannedWordList GetBannedWords()
        {
            return profanityService.GetBannedWordsForClient();
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
                ServerException.HandleException(ex, nameof(ExecuteGameAction));
            }
        }
    }
}
