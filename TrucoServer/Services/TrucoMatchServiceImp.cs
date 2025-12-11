using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Match;
using TrucoServer.Helpers.Profanity;
using TrucoServer.Helpers.Ranking;
using TrucoServer.Helpers.Security;
using TrucoServer.Langs;
using TrucoServer.Utilities;

namespace TrucoServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class TrucoMatchServiceImp : ITrucoMatchService
    {
        private const string STATUS_PUBLIC = "Public";
        private const string STATUS_PRIVATE = "Private";
        private const string STATUS_CLOSED = "Closed";
        private const string STATUS_PENDING = "Pending";
        private const string STATUS_EXPIRED = "Expired";
        private const string GUEST_PREFIX = "Guest_";
        private const int MILLISECONDS_DELAY = 100;

        private readonly IGameRegistry gameRegistry;
        private readonly IJoinService joinService;
        private readonly IEmailSender emailSender;
        private readonly ILobbyCoordinator lobbyCoordinator;
        private readonly ILobbyRepository lobbyRepository;
        private readonly IMatchCodeGenerator codeGenerator;
        private readonly IMatchStarter matchStarter;
        private readonly IProfanityServerService profanityService;
        private readonly BanService banService;

        private readonly baseDatosTrucoEntities context;

        public TrucoMatchServiceImp()
        {
            this.context = new baseDatosTrucoEntities();
            var coordinator = new LobbyCoordinator(context);
            var registry = new GameRegistry();
            var repository = new LobbyRepository(context);
            var bannedWordRepository = new BannedWordRepository();
            var statsService = new UserStatsService(context);
            var gameManager = new TrucoGameManager(context, statsService);
            var shuffler = new DefaultDeckShuffler();
            var participantBuilder = new GamePlayerBuilder(context, coordinator);

            var matchStarterDependencies = new MatchStarterDependencies
            {
                Context = context,
                GameRegistry = registry,
                Coordinator = coordinator,
                Repository = repository,
                Shuffler = shuffler,
                GameManager = gameManager,
                ParticipantBuilder = participantBuilder
            };
            var starter = new MatchStarter(matchStarterDependencies);

            var dependencies = new TrucoMatchServiceDependencies
            {
                GameRegistry = registry,
                JoinService = new JoinService(context, coordinator, repository),
                LobbyCoordinator = coordinator,
                LobbyRepository = repository,
                CodeGenerator = new MatchCodeGenerator(),
                Starter = starter,
                ProfanityService = new ProfanityServerService(bannedWordRepository),
                EmailSender = new EmailSender()
            };

            this.gameRegistry = dependencies.GameRegistry;
            this.joinService = dependencies.JoinService;
            this.lobbyCoordinator = dependencies.LobbyCoordinator;
            this.lobbyRepository = dependencies.LobbyRepository;
            this.codeGenerator = dependencies.CodeGenerator;
            this.matchStarter = dependencies.Starter;
            this.profanityService = dependencies.ProfanityService;
            this.emailSender = dependencies.EmailSender;

            this.banService = new BanService(context);
            this.profanityService.LoadBannedWords();
        }

        public TrucoMatchServiceImp(baseDatosTrucoEntities context, TrucoMatchServiceDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            this.context = context;

            this.gameRegistry = dependencies.GameRegistry;
            this.joinService = dependencies.JoinService;
            this.lobbyCoordinator = dependencies.LobbyCoordinator;
            this.lobbyRepository = dependencies.LobbyRepository;
            this.codeGenerator = dependencies.CodeGenerator;
            this.matchStarter = dependencies.Starter;
            this.profanityService = dependencies.ProfanityService;
            this.emailSender = dependencies.EmailSender;
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
                
                var host = context.User.FirstOrDefault(u => u.username == hostUsername);

                if (host == null)
                {
                    throw new InvalidOperationException("Host user not found.");
                }

                int versionId = lobbyRepository.ResolveVersionId(maxPlayers);
                string normalizedStatus = privacy.Equals("public", StringComparison.OrdinalIgnoreCase) ? STATUS_PUBLIC : STATUS_PRIVATE;

                var lobbyOptions = new LobbyCreationOptions
                {
                    Host = host,
                    VersionId = versionId,
                    MaxPlayers = maxPlayers,
                    Status = normalizedStatus
                };

                var lobby = lobbyRepository.CreateNewLobby(lobbyOptions);

                if (lobby == null)
                {
                    return string.Empty;
                }

                lobbyRepository.AddLobbyOwner(lobby, host);

                context.SaveChanges();

                lobbyCoordinator.RegisterLobbyMapping(matchCode, lobby);
                lobbyCoordinator.GetOrCreateLobbyLock(lobby.lobbyID);

                return matchCode;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(CreateLobby));
                return string.Empty;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(CreateLobby));
                return string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(CreateLobby));
                return string.Empty;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CreateLobby));
                return string.Empty;
            }
        }

        public int JoinMatch(string matchCode, string player)
        {
            var contextData = new JoinMatchContext { MatchCode = matchCode, PlayerUsername = player };

            if (!IsInputValid(contextData))
            {
                return 0;
            }

            try
            {
                Lobby lobby = ResolveLobby(matchCode);

                if (IsLobbyUnjoinable(lobby))
                {
                    return 0;
                }

                var accessResult = ValidateLobbyAccess(lobby, contextData);

                if (!accessResult.IsAllowed)
                {
                    return 0;
                }

                bool joinSuccess = ExecuteSafeJoin(lobby, contextData);

                if (joinSuccess && accessResult.UsedInvitation != null)
                {
                    ExpireInvitation(accessResult.UsedInvitation);
                }

                return joinSuccess ? lobby.maxPlayers : 0;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatch));
                throw FaultFactory.CreateFault("ServerDBErrorJoin", Lang.ExceptionTextDBErrorJoin);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatch));
                throw FaultFactory.CreateFault("ServerDBErrorJoin", Lang.ExceptionTextDBErrorJoin);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatch));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatch));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
        }

        private static bool IsInputValid(JoinMatchContext data)
        {
            return ServerValidator.IsMatchCodeValid(data.MatchCode) &&
                   ServerValidator.IsUsernameValid(data.PlayerUsername);
        }

        private static bool IsLobbyUnjoinable(Lobby lobby)
        {
            return lobby == null ||
                   string.Equals(lobby.status, STATUS_CLOSED, StringComparison.OrdinalIgnoreCase);
        }

        private AccessMatchResult ValidateLobbyAccess(Lobby lobby, JoinMatchContext data)
        {
            if (!string.Equals(lobby.status, STATUS_PRIVATE, StringComparison.OrdinalIgnoreCase))
            {
                return new AccessMatchResult { IsAllowed = true, UsedInvitation = null };
            }

            if (data.PlayerUsername.StartsWith(GUEST_PREFIX))
            {
                return new AccessMatchResult { IsAllowed = false };
            }

            Invitation invite = ValidatePrivateMatchAccess(data.MatchCode, data.PlayerUsername);

            return new AccessMatchResult
            {
                IsAllowed = invite != null,
                UsedInvitation = invite
            };
        }

        private bool ExecuteSafeJoin(Lobby lobby, JoinMatchContext data)
        {
            object lobbyLock = lobbyCoordinator.GetOrCreateLobbyLock(lobby.lobbyID);

            lock (lobbyLock)
            {
                return joinService.ProcessSafeJoin(lobby.lobbyID, data.MatchCode, data.PlayerUsername);
            }
        }

        private Invitation ValidatePrivateMatchAccess(string matchCode, string username)
        {
            var user = context.User.FirstOrDefault(u => u.username == username);
            if (user == null)
            {
                return null;
            }

            int numericCode = codeGenerator.GenerateNumericCodeFromString(matchCode);

            var invitation = context.Invitation.FirstOrDefault(i =>
                i.receiverEmail == user.email &&
                i.code == numericCode &&
                i.status == STATUS_PENDING);

            if (invitation == null)
            {
                return null;
            }

            if (invitation.expiresAt <= DateTime.Now)
            {
                return null;
            }

            return invitation;
        }

        private void ExpireInvitation(Invitation invitation)
        {
            try
            {
                invitation.status = STATUS_EXPIRED;
                context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitation));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitation));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitation));
            }
        }

        public void LeaveMatch(string matchCode, string player)
        {
            if (!ServerValidator.IsMatchCodeValid(matchCode) || !ServerValidator.IsUsernameValid(player))
            {
                return;
            }

            var contextData = new LeaveMatchContext { MatchCode = matchCode, PlayerUsername = player };

            try
            {
                ResolveMatchEntities(contextData);

                if (contextData.Lobby == null || contextData.User == null)
                {
                    return;
                }

                RemoveLobbyMember(contextData.Lobby.lobbyID, contextData.User.userID);

                lobbyCoordinator.NotifyPlayerLeft(matchCode, player);

                ProcessLobbyCleanup(contextData.Lobby.lobbyID, matchCode);
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatch));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatch));
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatch));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatch));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatch));
            }
        }

        private void ResolveMatchEntities(LeaveMatchContext ctx)
        {
            if (lobbyCoordinator.TryGetLobbyIdFromCode(ctx.MatchCode, out int id))
            {
                ctx.Lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
            }

            if (ctx.Lobby == null)
            {
                var criteria = new LobbyLeaveCriteria { MatchCode = ctx.MatchCode, Username = ctx.PlayerUsername };
                var result = lobbyRepository.ResolveLobbyForLeave(criteria);

                if (result != null)
                {
                    ctx.Lobby = result.Lobby;
                    ctx.User = result.Player;
                }
            }

            if (ctx.User == null)
            {
                ctx.User = context.User.FirstOrDefault(u => u.username == ctx.PlayerUsername);
            }
        }

        private void RemoveLobbyMember(int lobbyId, int userId)
        {
            var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobbyId && lm.userID == userId);

            if (member != null)
            {
                context.LobbyMember.Remove(member);
                context.SaveChanges();
            }
        }

        private void ProcessLobbyCleanup(int lobbyId, string matchCode)
        {
            if (context.LobbyMember.Any(lm => lm.lobbyID == lobbyId))
            {
                return;
            }

            lobbyRepository.CloseLobbyById(lobbyId);
            lobbyRepository.ExpireInvitationByMatchCode(matchCode);
            lobbyRepository.RemoveLobbyMembersById(lobbyId);
            matchStarter.HandleMatchStartupCleanup(matchCode);
        }

        public List<PublicLobbyInfo> GetPublicLobbies()
        {
            try
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetPublicLobbies));
                throw FaultFactory.CreateFault("ServerDBErrorGetPublicLobbies", Lang.ExceptionTextDBErrorGetPublicLobbies);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetPublicLobbies));
                throw FaultFactory.CreateFault("ServerDBErrorGetPublicLobbies", Lang.ExceptionTextDBErrorGetPublicLobbies);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetPublicLobbies));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetPublicLobbies));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
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
                    return match.Players.Select(p => new PlayerInfo
                    {
                        Username = p.Username,
                        Team = p.Team,
                        AvatarId = matchStarter.GetAvatarIdForPlayer(p.Username),
                        OwnerUsername = matchStarter.GetOwnerUsername(matchCode)
                    }).ToList();
                }

                Lobby lobby = null;

                if (lobbyCoordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                {
                    lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
                }

                if (lobby == null)
                {
                    lobby = lobbyRepository.FindLobbyByMatchCode(matchCode, false);
                }

                if (lobby == null)
                {
                    return lobbyCoordinator.GetGuestPlayersFromMemory(matchCode);
                }

                string ownerUsername = lobbyRepository.GetLobbyOwnerName(lobby.ownerID);
                var dbPlayers = lobbyRepository.GetDatabasePlayers(lobby, ownerUsername);
                var guestPlayers = lobbyCoordinator.GetGuestPlayersFromMemory(matchCode, ownerUsername);

                dbPlayers.AddRange(guestPlayers.Where(g => !dbPlayers.Any(p => p.Username == g.Username)));

                return dbPlayers;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyPlayers));
                throw FaultFactory.CreateFault("ServerDBErrorGetLobbyPlayers", Lang.ExceptionTextDBErrorGetLobbyPlayers);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyPlayers));
                throw FaultFactory.CreateFault("ServerDBErrorGetLobbyPlayers", Lang.ExceptionTextDBErrorGetLobbyPlayers);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyPlayers));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyPlayers));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
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
                var validation = matchStarter.ValidateMatchStart(matchCode);

                if (!validation.IsValid)
                {
                    return;
                }

                List<PlayerInfo> playersList = GetLobbyPlayers(matchCode);

                if (playersList.Count != validation.ExpectedPlayers)
                {
                    return;
                }

                matchStarter.InitiateMatchSequence(matchCode, validation.LobbyId, playersList);
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(StartMatch));
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(StartMatch));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(StartMatch));
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
                var callback = OperationContext.Current?.GetCallbackChannel<ITrucoCallback>();
                if (callback == null)
                {
                    return;
                }

                lobbyCoordinator.RemoveInactiveCallbacks(matchCode);

                bool isNew = lobbyCoordinator.RegisterChatCallback(matchCode, player, callback);

                if (isNew)
                {
                    Task.Delay(MILLISECONDS_DELAY).ContinueWith(_ =>
                    {
                        try
                        {
                            lobbyCoordinator.NotifyPlayerJoined(matchCode, player);
                        }
                        catch (Exception ex)
                        {
                            ServerException.HandleException(ex, nameof(JoinMatchChat) + "_NotifyTask");
                        }
                    });
                }
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatchChat));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(JoinMatchChat));
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
                var callback = OperationContext.Current?.GetCallbackChannel<ITrucoCallback>();
                if (callback == null)
                {
                    return;
                }

                lobbyCoordinator.RemoveCallbackFromMatch(matchCode, callback);
                lobbyCoordinator.RemoveMatchCallbackMapping(callback);

                lobbyCoordinator.NotifyPlayerLeft(matchCode, player);
                gameRegistry.AbortAndRemoveGame(matchCode, player);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatchChat));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(LeaveMatchChat));
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
                    bool shouldBan = BanService.RegisterOffense(player);

                    if (shouldBan)
                    {
                        KickAndBanPlayer(matchCode, player);
                        return;
                    }

                    message = profanityService.CensorText(message);
                }
                else
                {
                    BanService.ResetOffenses(player);
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
                            /**
                             * Exceptions from individual client callbacks are 
                             * intentionally ignored here to prevent a single failure
                             * from halting the broadcast to other clients in the match. 
                             * This ensures reliable message delivery
                             * to all active participants without interrupting 
                             * the overall chat functionality.
                             */
                        }
                    }
                });
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(SendChatMessage));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(SendChatMessage));
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
                    lobbyCoordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
                        cb.OnPlayerJoined(matchCode, username));
                }
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(SwitchTeam));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(SwitchTeam));
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(SwitchTeam));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(SwitchTeam));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SwitchTeam));
            }
        }

        public bool InviteFriend(InviteFriendOptions inviteContext)
        {
            if (!ServerValidator.IsMatchCodeValid(inviteContext.MatchCode) ||
                !ServerValidator.IsUsernameValid(inviteContext.SenderUsername) ||
                !ServerValidator.IsUsernameValid(inviteContext.FriendUsername))
            {
                return false;
            }

            try
            {
                if (!TryGetUsersForInvitation(inviteContext.SenderUsername, inviteContext.FriendUsername, out User senderUser, out User friendUser))
                {
                    return false;
                }

                if (IsFriendAlreadyInLobby(inviteContext.MatchCode, friendUser.userID))
                {
                    return false;
                }

                CreateOrUpdateInvitationRecord(senderUser.userID, friendUser.email, inviteContext.MatchCode);

                var emailOptions = new InviteFriendData
                {
                    MatchCode = inviteContext.MatchCode,
                    SenderUser = senderUser,
                    FriendUser = friendUser
                };

                emailSender.SendInvitationEmailAsync(emailOptions);

                return true;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(InviteFriend));
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(InviteFriend));
                return false;
            }
            catch (InvalidOperationException ex)
            { 
                ServerException.HandleException(ex, nameof(InviteFriend));
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(InviteFriend));
                return false;
            }
        }

        private bool TryGetUsersForInvitation(string senderName, string friendName, out User sender, out User friend)
        {
            sender = null;
            friend = null;

            var senderUser = context.User.FirstOrDefault(u => u.username == senderName);
            var friendUser = context.User.FirstOrDefault(u => u.username == friendName);

            if (senderUser != null && friendUser != null && !string.IsNullOrEmpty(friendUser.email))
            {
                sender = senderUser;
                friend = friendUser;
                return true;
            }
            return false;
        }

        private bool IsFriendAlreadyInLobby(string matchCode, int friendUserId)
        {
            if (lobbyCoordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
            {
                return context.LobbyMember.Any(lm => lm.lobbyID == lobbyId && lm.userID == friendUserId);
            }
            return false;
        }

        private void CreateOrUpdateInvitationRecord(int senderId, string friendEmail, string matchCode)
        {
            int numericCode = codeGenerator.GenerateNumericCodeFromString(matchCode);

            var existingInv = context.Invitation.FirstOrDefault(i =>
                i.senderID == senderId &&
                i.receiverEmail == friendEmail &&
                i.code == numericCode &&
                i.status == STATUS_PENDING);

            if (existingInv == null)
            {
                var invitation = new Invitation
                {
                    senderID = senderId,
                    receiverEmail = friendEmail,
                    code = numericCode,
                    status = STATUS_PENDING,
                    expiresAt = DateTime.Now.AddMinutes(30)
                };
                context.Invitation.Add(invitation);
            }
            else
            {
                existingInv.expiresAt = DateTime.Now.AddMinutes(30);
            }

            context.SaveChanges();
        }

        private void KickAndBanPlayer(string matchCode, string username)
        {
            try
            {
                banService.BanUser(username, "Toxicity in chat (5 ofenses)");

                if (lobbyCoordinator.TryGetActiveCallbackForPlayer(username, out var bannedUserCallback))
                {
                    try
                    {
                        bannedUserCallback.OnForcedLogout();
                    }
                    catch (Exception)
                    {
                        /** 
                         * Callback may fail if user is already 
                         * disconnected; ignore to continue 
                         * banning workflow 
                         */
                    }
                }
                if (gameRegistry.TryGetGame(matchCode, out _))
                {
                    gameRegistry.AbortAndRemoveGame(matchCode, username);
                }

                LeaveMatch(matchCode, username);
                LeaveMatchChat(matchCode, username);
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(KickAndBanPlayer));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(KickAndBanPlayer));
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(KickAndBanPlayer));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(KickAndBanPlayer));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(KickAndBanPlayer));
            }
        }

        private Lobby ResolveLobby(string matchCode)
        {
            Lobby lobby = null;

            if (lobbyCoordinator.TryGetLobbyIdFromCode(matchCode, out int id))
            {
                lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == id);
            }

            if (lobby == null)
            {
                lobby = lobbyRepository.ResolveLobbyForJoin(matchCode);

                if (lobby != null && !lobby.status.Equals(STATUS_CLOSED, StringComparison.OrdinalIgnoreCase))
                {
                    lobbyCoordinator.RegisterLobbyMapping(matchCode, lobby);
                }
            }

            return lobby;
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
                if (action == null)
                {
                    throw new ArgumentNullException(nameof(action));
                }

                if (matchStarter.GetMatchAndPlayerID(matchCode, out TrucoMatch match, out int playerID))
                {
                    action(match, playerID);
                }
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, callerName);
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, callerName);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, callerName);
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, callerName);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, callerName);
            }
        }
    }
}
