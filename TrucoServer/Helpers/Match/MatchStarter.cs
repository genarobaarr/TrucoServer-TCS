using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Match
{
    public class MatchStarter : IMatchStarter
    {
        private const string GUEST_PREFIX = "Guest_";

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

        private void ProcessGuestPlayer(PlayerInfo pInfo, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            int guestTempId = -Math.Abs(pInfo.Username.GetHashCode());
            gamePlayers.Add(new PlayerInformation(guestTempId, pInfo.Username, pInfo.Team));

            if (coordinator.TryGetActiveCallbackForPlayer(pInfo.Username, out var guestCb))
            {
                gameCallbacks[guestTempId] = guestCb;
            }
            else
            {
                Console.WriteLine($"[WARNING] Guest {pInfo.Username} not found or disconnected during start.");
            }
        }

        private void ProcessRegisteredPlayer(baseDatosTrucoEntities context, PlayerInfo pInfo, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            var user = context.User.FirstOrDefault(u => u.username == pInfo.Username);

            if (user != null)
            {
                gamePlayers.Add(new PlayerInformation(user.userID, user.username, pInfo.Team));

                if (coordinator.TryGetActiveCallbackForPlayer(pInfo.Username, out ITrucoCallback activeCallback))
                {
                    gameCallbacks[user.userID] = activeCallback;
                }
                else
                {
                    Console.WriteLine($"[WARNING] User {pInfo.Username} is in lobby but has no active connection.");
                }
            }
        }

        public void InitializeAndRegisterGame(string matchCode, int lobbyId, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            try
            {
                var newDeck = new Deck(shuffler);
                var newGame = new TrucoMatch(matchCode, lobbyId, gamePlayers, gameCallbacks, newDeck, gameManager);

                if (!gameRegistry.TryAddGame(matchCode, newGame))
                {
                    LogManager.LogError(new Exception($"Failed to add running game {matchCode}"), nameof(InitializeAndRegisterGame));
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(InitializeAndRegisterGame));
            }
        }

        public void NotifyMatchStart(string matchCode, List<PlayerInfo> players)
        {
            try
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

                Console.WriteLine($"[SERVER] Match {matchCode} started.");
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
                LogManager.LogError(ex, nameof(HandleMatchStartupCleanup));
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
                LogManager.LogError(ex, nameof(GetMatchAndPlayerID));
                return false;
            }
        }
    }
}