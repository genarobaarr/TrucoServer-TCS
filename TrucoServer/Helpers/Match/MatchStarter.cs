using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Match
{
    public class MatchStarter : IMatchStarter
    {
        private const string GUEST_PREFIX = "Guest_";
        private const string TEAM_1 = "Team 1";
        private const string DEFAULT_AVATAR_NAME = "avatar_aaa_default";

        private readonly IGameRegistry gameRegistry;
        private readonly ILobbyCoordinator coordinator;
        private readonly ILobbyRepository repository;
        private readonly IDeckShuffler shuffler;
        private readonly IGameManager gameManager;

        public MatchStarter(
            IGameRegistry gameRegistry,
            ILobbyCoordinator coordinator,
            ILobbyRepository repository,
            IDeckShuffler shuffler,
            IGameManager gameManager)
        {
            this.gameRegistry = gameRegistry;
            this.coordinator = coordinator;
            this.repository = repository;
            this.shuffler = shuffler;
            this.gameManager = gameManager;
        }

        public bool BuildGamePlayersAndCallbacks(List<PlayerInfo> playersList, out List<PlayerInformation> gamePlayers, out Dictionary<int, ITrucoCallback> gameCallbacks)
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
                            if (!ProcessGuestPlayer(pInfo, gamePlayers, gameCallbacks))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!ProcessRegisteredPlayer(context, pInfo, gamePlayers, gameCallbacks))
                            {
                                return false;
                            }
                        }
                    }
                }

                if (gamePlayers.Count != gameCallbacks.Count)
                {
                    LogManager.LogError(new Exception($"Discrepancy: {gamePlayers.Count} players vs {gameCallbacks.Count} connections."), nameof(BuildGamePlayersAndCallbacks));
                    return false;
                }

                return gamePlayers.Count == gameCallbacks.Count && gamePlayers.Count > 0;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(BuildGamePlayersAndCallbacks));
                return false;
            }
        }

        private bool ProcessGuestPlayer(PlayerInfo pInfo, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            try
            {
                int guestTempId = (int)-Math.Abs((long)pInfo.Username.GetHashCode());

                if (coordinator.TryGetActiveCallbackForPlayer(pInfo.Username, out var guestCb))
                {
                    var registeredInfo = coordinator.GetPlayerInfoFromCallback(guestCb);
                    string team = registeredInfo?.Team ?? pInfo.Team ?? TEAM_1;

                    gamePlayers.Add(new PlayerInformation(guestTempId, pInfo.Username, pInfo.Team));
                    gameCallbacks[guestTempId] = guestCb;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ProcessGuestPlayer));
                return false;
            }
        }

        private bool ProcessRegisteredPlayer(baseDatosTrucoEntities context, PlayerInfo pInfo, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            try
            {
                var user = context.User.FirstOrDefault(u => u.username == pInfo.Username);

                if (user != null)
                {
                    if (coordinator.TryGetActiveCallbackForPlayer(pInfo.Username, out ITrucoCallback activeCallback))
                    {
                        gamePlayers.Add(new PlayerInformation(user.userID, user.username, pInfo.Team));
                        gameCallbacks[user.userID] = activeCallback;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ProcessRegisteredPlayer));
                return false;
            }
        }

        public MatchStartValidation ValidateMatchStart(string matchCode)
        {
            var result = new MatchStartValidation { IsValid = false };

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = null;

                    if (coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                    {
                        lobby = context.Lobby.Find(id);
                    }

                    if (lobby == null)
                    {
                        lobby = repository.FindLobbyByMatchCode(context, matchCode, true);
                    }

                    if (lobby == null)
                    {
                        return result;
                    }

                    int dbCount = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
                    int guestCount = coordinator.GetGuestCountInMemory(matchCode);
                    int totalPlayers = dbCount + guestCount;

                    if (totalPlayers == lobby.maxPlayers)
                    {
                        result.IsValid = true;
                        result.LobbyId = lobby.lobbyID;
                        result.ExpectedPlayers = lobby.maxPlayers;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ValidateMatchStart));
            }

            return result;
        }

        public void InitiateMatchSequence(string matchCode, int lobbyId, List<PlayerInfo> playersList)
        {
            if (!BuildGamePlayersAndCallbacks(playersList, out var gamePlayers, out var gameCallbacks))
            {
                return;
            }

            var initOptions = new GameInitializationOptions
            {
                MatchCode = matchCode,
                LobbyId = lobbyId,
                GamePlayers = gamePlayers,
                GameCallbacks = gameCallbacks
            };

            InitializeAndRegisterGame(initOptions);
            NotifyMatchStart(matchCode, playersList);
            HandleMatchStartupCleanup(matchCode);

            Task.Delay(500).ContinueWith(_ =>
            {
                if (gameRegistry.TryGetGame(matchCode, out var match))
                {
                    match.StartNewHand();
                }
            });
        }

        public void InitializeAndRegisterGame(GameInitializationOptions options)
        {
            try
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                string ownerUsername = null;

                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = context.Lobby.Find(options.LobbyId);

                    if (lobby == null)
                    {
                        LogManager.LogError(new Exception($"Lobby {options.LobbyId} not found"), nameof(InitializeAndRegisterGame));
                        return;
                    }

                    var owner = context.User.Find(lobby.ownerID);
                    ownerUsername = owner?.username;

                    if (string.IsNullOrEmpty(ownerUsername))
                    {
                        ownerUsername = options.GamePlayers.FirstOrDefault()?.Username;
                    }
                }

                var orderedPlayers = OrderPlayersForMatch(options.GamePlayers, ownerUsername);

                if (orderedPlayers.Count != options.GamePlayers.Count)
                {
                    orderedPlayers = options.GamePlayers;
                }

                var newDeck = new Deck(shuffler);

                var matchContext = new TrucoMatchContext
                {
                    MatchCode = options.MatchCode,
                    LobbyId = options.LobbyId,
                    Players = orderedPlayers,
                    Callbacks = options.GameCallbacks,
                    Deck = newDeck,
                    GameManager = gameManager
                };

                var newGame = new TrucoMatch(matchContext);

                if (!gameRegistry.TryAddGame(options.MatchCode, newGame))
                {
                    LogManager.LogError(new Exception($"Failed to add running game {options.MatchCode}"), nameof(InitializeAndRegisterGame));
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(InitializeAndRegisterGame));
            }
        }

        private List<PlayerInformation> OrderPlayersForMatch(List<PlayerInformation> players, string ownerUsername)
        {
            if (players == null || players.Count == 0)
            {
                return players;
            }

            if (players.Count == 2)
            {
                var owner = players.FirstOrDefault(p => p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));
                var opponent = players.FirstOrDefault(p => !p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));

                if (owner == null || opponent == null)
                {
                    return players;
                }

                return new List<PlayerInformation> { owner, opponent };
            }

            var owner4 = players.FirstOrDefault(p => p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));

            if (owner4 == null)
            {
                return players;
            }

            var ownerTeam = owner4.Team;
            var teammates = players.Where(p => p.Team == ownerTeam && p.Username != ownerUsername).ToList();
            var opponents = players.Where(p => p.Team != ownerTeam).OrderBy(p => p.Username).ToList();

            if (teammates.Count == 0)
            {
                return players;
            }

            if (opponents.Count < 2)
            {
                return players;
            }

            var orderedPlayers = new List<PlayerInformation>
            {
                owner4,        
                opponents[0],
                teammates[0],
                opponents[1] 
            };

            return orderedPlayers;
        }

        public void NotifyMatchStart(string matchCode, List<PlayerInfo> players)
        {
            try
            {
                if (gameRegistry.TryGetGame(matchCode, out var match))
                {
                    var orderedPlayers = match.Players.Select(p => new PlayerInfo
                    {
                        Username = p.Username,
                        Team = p.Team,
                        AvatarId = GetAvatarIdForPlayer(p.Username),
                        OwnerUsername = GetOwnerUsername(matchCode)
                    }).ToList();

                    coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
                    {
                        try
                        {
                            cb.OnMatchStarted(matchCode, orderedPlayers);
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogError(ex, nameof(NotifyMatchStart));
                        }
                    });
                }
                else
                {
                    coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
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
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(NotifyMatchStart));
            }
        }

        public void HandleMatchStartupCleanup(string matchCode)
        {
            try
            {
                if (coordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
                {
                    repository.CloseLobbyById(lobbyId);
                    repository.ExpireInvitationByMatchCode(matchCode);
                    repository.RemoveLobbyMembersById(lobbyId);

                    coordinator.RemoveLobbyMapping(matchCode); 
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(HandleMatchStartupCleanup));
            }
        }

        public bool GetMatchAndPlayerID(string matchCode, out TrucoMatch match, out int playerID)
        {
            match = null;
            playerID = -1;


            if (!gameRegistry.TryGetGame(matchCode, out match))
            {
                return false;
            }

            var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

            var playerInfo = coordinator.GetPlayerInfoFromCallback(callback);

            if (callback == null || playerInfo == null)
            {
                return false;
            }

            if (playerInfo.Username.StartsWith(GUEST_PREFIX))
            {
                playerID = -Math.Abs(playerInfo.Username.GetHashCode());
                
                return true;
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
                ServerException.HandleException(ex, nameof(GetMatchAndPlayerID));
                return false;
            }
        }

        public string GetAvatarIdForPlayer(string username)
        {
            try
            {
                if (username.StartsWith(GUEST_PREFIX))
                {
                    return DEFAULT_AVATAR_NAME;
                }

                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.username == username);
                    
                    if (user != null)
                    {
                        var profile = context.UserProfile.FirstOrDefault(up => up.userID == user.userID);
                       
                        return profile?.avatarID ?? DEFAULT_AVATAR_NAME;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetAvatarIdForPlayer));
            }

            return DEFAULT_AVATAR_NAME;
        }

        public string GetOwnerUsername(string matchCode)
        {
            try
            {
                if (coordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
                {
                    using (var context = new baseDatosTrucoEntities())
                    {
                        var lobby = context.Lobby.Find(lobbyId);
                        if (lobby != null)
                        {
                            var owner = context.User.Find(lobby.ownerID);
                            return owner?.username;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetOwnerUsername));
            }

            return null;
        }
    }
}