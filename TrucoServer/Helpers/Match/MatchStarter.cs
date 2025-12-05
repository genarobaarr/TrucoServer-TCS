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

        private readonly MatchStarterDependencies dependencies;

        public MatchStarter(MatchStarterDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (dependencies.Context == null)
            {
                throw new ArgumentNullException(nameof(dependencies.Context));
            }

            if (dependencies.Coordinator == null)
            {
                throw new ArgumentNullException(nameof(dependencies.Coordinator));
            }

            this.dependencies = dependencies;
        }

        public bool BuildGamePlayersAndCallbacks(List<PlayerInfo> playersList, out List<PlayerInformation> gamePlayers, out Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            var result = dependencies.ParticipantBuilder.BuildParticipants(playersList);
            gamePlayers = result.Players;
            gameCallbacks = result.Callbacks;
            return result.IsSuccess;
        }

        public MatchStartValidation ValidateMatchStart(string matchCode)
        {
            var result = new MatchStartValidation { IsValid = false };

            try
            {
                Lobby lobby = null;

                if ( dependencies.Coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                {
                    lobby = dependencies.Context.Lobby.Find(id);
                }

                if (lobby == null)
                {
                    lobby =  dependencies.Repository.FindLobbyByMatchCode(matchCode, true);
                }

                if (lobby == null)
                {
                    return result;
                }

                int dbCount = dependencies.Context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
                int guestCount =  dependencies.Coordinator.GetGuestCountInMemory(matchCode);
                int totalPlayers = dbCount + guestCount;

                if (totalPlayers == lobby.maxPlayers)
                {
                    result.IsValid = true;
                    result.LobbyId = lobby.lobbyID;
                    result.ExpectedPlayers = lobby.maxPlayers;
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
            var participantsResult = dependencies.ParticipantBuilder.BuildParticipants(playersList);

            if (!participantsResult.IsSuccess)
            {
                return;
            }

            var initOptions = new GameInitializationOptions
            {
                MatchCode = matchCode,
                LobbyId = lobbyId,
                GamePlayers = participantsResult.Players,
                GameCallbacks = participantsResult.Callbacks
            };

            InitializeAndRegisterGame(initOptions);
            NotifyMatchStart(matchCode, playersList);
            HandleMatchStartupCleanup(matchCode);

            Task.Delay(500).ContinueWith(_ =>
            {
                if (dependencies.GameRegistry.TryGetGame(matchCode, out var match))
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

                string ownerName = GetOwnerUsernameByLobbyId(options.LobbyId);

                var orderedPlayers = dependencies.PositionService.DetermineTurnOrder(options.GamePlayers, ownerName);

                var newGame = CreateMatchInstance(options, orderedPlayers);

                RegisterGame(options.MatchCode, newGame);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(InitializeAndRegisterGame));
            }
        }

        public void NotifyMatchStart(string matchCode, List<PlayerInfo> players)
        {
            try
            {
                if (dependencies.GameRegistry.TryGetGame(matchCode, out var match))
                {
                    var orderedPlayers = match.Players.Select(p => new PlayerInfo
                    {
                        Username = p.Username,
                        Team = p.Team,
                        AvatarId = GetAvatarIdForPlayer(p.Username),
                        OwnerUsername = GetOwnerUsername(matchCode)
                    }).ToList();

                     dependencies.Coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
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
                     dependencies.Coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
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
                if ( dependencies.Coordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
                {
                     dependencies.Repository.CloseLobbyById(lobbyId);
                     dependencies.Repository.ExpireInvitationByMatchCode(matchCode);
                     dependencies.Repository.RemoveLobbyMembersById(lobbyId);

                     dependencies.Coordinator.RemoveLobbyMapping(matchCode); 
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(HandleMatchStartupCleanup));
            }
        }

        private TrucoMatch CreateMatchInstance(GameInitializationOptions options, List<PlayerInformation> orderedPlayers)
        {
            var newDeck = new Deck(dependencies.Shuffler);

            var matchContext = new TrucoMatchContext
            {
                MatchCode = options.MatchCode,
                LobbyId = options.LobbyId,
                Players = orderedPlayers,
                Callbacks = options.GameCallbacks,
                Deck = newDeck,
                GameManager = dependencies.GameManager
            };

            return new TrucoMatch(matchContext);
        }

        private void RegisterGame(string matchCode, TrucoMatch newGame)
        {
            if (!dependencies.GameRegistry.TryAddGame(matchCode, newGame))
            {
                LogManager.LogError(new Exception($"Failed to add running game {matchCode}"), nameof(InitializeAndRegisterGame));
            }
        }

        public bool GetMatchAndPlayerID(string matchCode, out TrucoMatch match, out int playerID)
        {
            match = null;
            playerID = -1;


            if (!dependencies.GameRegistry.TryGetGame(matchCode, out match))
            {
                return false;
            }

            var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

            var playerInfo =  dependencies.Coordinator.GetPlayerInfoFromCallback(callback);

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
                var user = dependencies.Context.User.FirstOrDefault(u => u.username == playerInfo.Username);

                if (user == null)
                {
                    return false;
                }

                playerID = user.userID;
                    
                return true;
                
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

                var user = dependencies.Context.User.FirstOrDefault(u => u.username == username);
                    
                if (user != null)
                {
                    var profile = dependencies.Context.UserProfile.FirstOrDefault(up => up.userID == user.userID);
                       
                    return profile?.avatarID ?? DEFAULT_AVATAR_NAME;
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
                if ( dependencies.Coordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
                {
                    var lobby = dependencies.Context.Lobby.Find(lobbyId);
                    if (lobby != null)
                    {
                        var owner = dependencies.Context.User.Find(lobby.ownerID);
                        return owner?.username;
                    }
                }
                
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetOwnerUsername));
            }

            return null;
        }

        private string GetOwnerUsernameByLobbyId(int lobbyId)
        {
            try
            {
                var lobby = dependencies.Context.Lobby.Find(lobbyId);

                if (lobby == null)
                {
                    return null;
                }

                var owner = dependencies.Context.User.Find(lobby.ownerID);
                return owner?.username ?? string.Empty;
                

            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetOwnerUsernameByLobbyId));
            }

            return null;
        }
    }
}