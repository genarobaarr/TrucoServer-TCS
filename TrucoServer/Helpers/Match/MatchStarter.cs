using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
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
        private const string DEFAULT_AVATAR_NAME = "avatar_aaa_default";
        private const int MILLISECONDS_DELAY = 500;

        private readonly MatchStarterDependencies dependencies;

        public MatchStarter(MatchStarterDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (dependencies.Context == null)
            {
                throw new InvalidOperationException("Context dependency cannot be null");
            }

            if (dependencies.Coordinator == null)
            {
                throw new InvalidOperationException("Coordinator dependency cannot be null");
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
            var result = new MatchStartValidation 
            { 
                IsValid = false 
            };

            try
            {
                Lobby lobby = null;

                if (dependencies.Coordinator.TryGetLobbyIdFromCode(matchCode, out int id))
                {
                    lobby = dependencies.Context.Lobby.Find(id);
                }

                if (lobby == null)
                {
                    lobby = dependencies.Repository.FindLobbyByMatchCode(matchCode, true);
                }

                if (lobby == null)
                {
                    return result;
                }

                int dbCount = dependencies.Context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);

                int guestCount = dependencies.Coordinator.GetGuestCountInMemory(matchCode);

                int totalPlayers = dbCount + guestCount;

                if (totalPlayers == lobby.maxPlayers)
                {
                    result.IsValid = true;
                    result.LobbyId = lobby.lobbyID;
                    result.ExpectedPlayers = lobby.maxPlayers;
                }
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(ValidateMatchStart));
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(ValidateMatchStart));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(ValidateMatchStart));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ValidateMatchStart));
            }

            return result;
        }

        public void InitiateMatchSequence(string matchCode, int lobbyId, List<PlayerInfo> players)
        {
            var participantsResult = dependencies.ParticipantBuilder.BuildParticipants(players);

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
            NotifyMatchStart(matchCode, players);
            HandleMatchStartupCleanup(matchCode);

            Task.Delay(MILLISECONDS_DELAY).ContinueWith(_ =>
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

                var orderedPlayers = ListPositionService.DetermineTurnOrder(options.GamePlayers, ownerName);

                var newGame = CreateMatchInstance(options, orderedPlayers);

                RegisterGame(options.MatchCode, newGame);
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(InitializeAndRegisterGame));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(InitializeAndRegisterGame));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(InitializeAndRegisterGame));
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
                List<PlayerInfo> playersToNotify;

                if (dependencies.GameRegistry.TryGetGame(matchCode, out var match))
                {
                    playersToNotify = match.Players.Select(p => new PlayerInfo
                    {
                        Username = p.Username,
                        Team = p.Team,
                        AvatarId = GetAvatarIdForPlayer(p.Username),
                        OwnerUsername = GetOwnerUsername(matchCode)
                    }).ToList();
                }
                else
                {
                    playersToNotify = players;
                }

                dependencies.Coordinator.BroadcastToMatchCallbacksAsync(matchCode, cb =>
                {
                    try
                    {
                        cb.OnMatchStarted(matchCode, playersToNotify);
                    }
                    catch (TimeoutException ex)
                    {
                        ServerException.HandleException(ex, nameof(NotifyMatchStart));
                    }
                    catch (CommunicationException ex)
                    {
                        ServerException.HandleException(ex, nameof(NotifyMatchStart));
                    }
                    catch (Exception ex)
                    {
                        ServerException.HandleException(ex, nameof(NotifyMatchStart));
                    }
                });
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(NotifyMatchStart));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(NotifyMatchStart));
            }
        }

        public void HandleMatchStartupCleanup(string matchCode)
        {
            try
            {
                if (dependencies.Coordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
                {
                    dependencies.Repository.CloseLobbyById(lobbyId);
                    dependencies.Repository.ExpireInvitationByMatchCode(matchCode);
                    dependencies.Repository.RemoveLobbyMembersById(lobbyId);

                    dependencies.Coordinator.RemoveLobbyMapping(matchCode);
                }
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(HandleMatchStartupCleanup));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(HandleMatchStartupCleanup));
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
                ServerException.HandleException(new Exception($"Failed to add running game {matchCode}"), nameof(InitializeAndRegisterGame));
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

            var callback = OperationContext.Current?.GetCallbackChannel<ITrucoCallback>();

            if (callback == null)
            {
                return false;
            }

            var playerInfo = dependencies.Coordinator.GetPlayerInfoFromCallback(callback);

            if (playerInfo == null)
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetMatchAndPlayerID));
                
                return false;
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(GetMatchAndPlayerID));
                
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetMatchAndPlayerID));
               
                return false;
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
                if (string.IsNullOrEmpty(username))
                {
                    return DEFAULT_AVATAR_NAME;
                }

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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetAvatarIdForPlayer));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetAvatarIdForPlayer));
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
                if (dependencies.Coordinator.TryGetLobbyIdFromCode(matchCode, out int lobbyId))
                {
                    var lobby = dependencies.Context.Lobby.Find(lobbyId);
                   
                    if (lobby != null)
                    {
                        var owner = dependencies.Context.User.Find(lobby.ownerID);
                        
                        return owner?.username;
                    }
                }
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetOwnerUsername));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetOwnerUsername));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetOwnerUsername));
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetOwnerUsernameByLobbyId));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetOwnerUsernameByLobbyId));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetOwnerUsernameByLobbyId));
            }

            return null;
        }
    }
}