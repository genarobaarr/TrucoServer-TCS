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
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";

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
                Console.WriteLine($"[BUILD PLAYERS] Processing {playersList.Count} players:");

                foreach (var pInfo in playersList)
                {
                    Console.WriteLine($"  - {pInfo.Username} (Team: {pInfo.Team})");
                }

                using (var context = new baseDatosTrucoEntities())
                {
                    foreach (var pInfo in playersList)
                    {
                        if (pInfo.Username.StartsWith(GUEST_PREFIX))
                        {
                            if (!ProcessGuestPlayer(pInfo, gamePlayers, gameCallbacks))
                            {
                                Console.WriteLine($"[WARNING] Failed to process guest {pInfo.Username}");
                                return false;
                            }
                        }
                        else
                        {
                            if (!ProcessRegisteredPlayer(context, pInfo, gamePlayers, gameCallbacks))
                            {
                                Console.WriteLine($"[WARNING] Failed to process registered player {pInfo.Username}");
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

                Console.WriteLine($"[BUILD PLAYERS] Successfully built {gamePlayers.Count} players ({gamePlayers.Count(p => p.PlayerID < 0)} guests)");

                Console.WriteLine("[BUILD PLAYERS] Final player list:");
                for (int i = 0; i < gamePlayers.Count; i++)
                {
                    Console.WriteLine($"  [{i}] {gamePlayers[i].Username}: PlayerID={gamePlayers[i].PlayerID}, Team={gamePlayers[i].Team}");
                }

                return gamePlayers.Count == gameCallbacks.Count && gamePlayers.Count > 0;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(BuildGamePlayersAndCallbacks));
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
                    string team = registeredInfo?.Team ?? pInfo.Team ?? "Team 1";

                    gamePlayers.Add(new PlayerInformation(guestTempId, pInfo.Username, pInfo.Team));
                    gameCallbacks[guestTempId] = guestCb;

                    Console.WriteLine($"[GUEST PROCESSING] Guest {pInfo.Username} added with ID {guestTempId}, Team: {pInfo.Team}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[WARNING] Guest {pInfo.Username} not found or disconnected during start.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ProcessGuestPlayer));
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

                        Console.WriteLine($"[USER PROCESSING] User {pInfo.Username} added with ID {user.userID}, Team: {pInfo.Team}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] User {pInfo.Username} is in lobby but has no active connection.");
                        return false;
                    }
                }
                Console.WriteLine($"[ERROR] User {pInfo.Username} not found in database");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ProcessRegisteredPlayer));
                return false;
            }
        }

        public void InitializeAndRegisterGame(string matchCode, int lobbyId, List<PlayerInformation> gamePlayers, Dictionary<int, ITrucoCallback> gameCallbacks)
        {
            try
            {
                Console.WriteLine($"[MATCH INIT] Starting game initialization for {matchCode}");
                Console.WriteLine($"[MATCH INIT] Received {gamePlayers.Count} players:");

                foreach (var p in gamePlayers)
                {
                    Console.WriteLine($"  - {p.Username} (ID: {p.PlayerID}, Team: {p.Team})");
                }

                string ownerUsername = null;

                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = context.Lobby.Find(lobbyId);

                    if (lobby == null)
                    {
                        LogManager.LogError(new Exception($"Lobby {lobbyId} not found"), nameof(InitializeAndRegisterGame));
                        return;
                    }

                    var owner = context.User.Find(lobby.ownerID);
                    ownerUsername = owner?.username;

                    if (string.IsNullOrEmpty(ownerUsername))
                    {
                        Console.WriteLine($"[MATCH INIT ERROR] Could not determine owner for lobby {lobbyId}");
                        ownerUsername = gamePlayers.FirstOrDefault()?.Username;
                    }

                    Console.WriteLine($"[MATCH INIT] Lobby owner: {ownerUsername}");
                }

                var orderedPlayers = OrderPlayersForMatch(gamePlayers, ownerUsername);

                if (orderedPlayers.Count != gamePlayers.Count)
                {
                    Console.WriteLine($"[MATCH INIT ERROR] Player count mismatch! Original: {gamePlayers.Count}, Ordered: {orderedPlayers.Count}");
                    Console.WriteLine("[MATCH INIT] Using original order as fallback");
                    orderedPlayers = gamePlayers;
                }

                Console.WriteLine($"[MATCH INIT] Final ordered players for match {matchCode}:");
                for (int i = 0; i < orderedPlayers.Count; i++)
                {
                    Console.WriteLine($"  Position {i}: {orderedPlayers[i].Username} (ID: {orderedPlayers[i].PlayerID}, Team: {orderedPlayers[i].Team})");
                }

                var newDeck = new Deck(shuffler);
                var newGame = new TrucoMatch(matchCode, lobbyId, orderedPlayers, gameCallbacks, newDeck, gameManager);

                if (!gameRegistry.TryAddGame(matchCode, newGame))
                {
                    LogManager.LogError(new Exception($"Failed to add running game {matchCode}"), nameof(InitializeAndRegisterGame));
                }
                else
                {
                    Console.WriteLine($"[MATCH INIT] Game {matchCode} successfully registered");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(InitializeAndRegisterGame));
            }
        }

        private List<PlayerInformation> OrderPlayersForMatch(List<PlayerInformation> players, string ownerUsername)
        {
            if (players == null || players.Count == 0)
            {
                Console.WriteLine("[ORDER ERROR] Players list is null or empty");
                return players;
            }

            if (players.Count == 2)
            {
                var owner = players.FirstOrDefault(p => p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));
                var opponent = players.FirstOrDefault(p => !p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));

                if (owner == null || opponent == null)
                {
                    Console.WriteLine("[ORDER ERROR] Could not find owner or opponent for 1v1");
                    return players;
                }

                return new List<PlayerInformation> { owner, opponent };
            }

            var owner4 = players.FirstOrDefault(p => p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));

            if (owner4 == null)
            {
                Console.WriteLine($"[ORDER ERROR] Owner {ownerUsername} not found in players list");

                Console.WriteLine($"[ORDER DEBUG] Available players:");
                foreach (var p in players)
                {
                    Console.WriteLine($"  - {p.Username} ({p.Team})");
                }

                return players;
            }

            var ownerTeam = owner4.Team;
            var teammates = players.Where(p => p.Team == ownerTeam && p.Username != ownerUsername).ToList();
            var opponents = players.Where(p => p.Team != ownerTeam).OrderBy(p => p.Username).ToList();

            Console.WriteLine($"[ORDER DEBUG] Owner: {owner4.Username} ({ownerTeam})");
            Console.WriteLine($"[ORDER DEBUG] Teammates: {string.Join(", ", teammates.Select(t => t.Username))}");
            Console.WriteLine($"[ORDER DEBUG] Opponents: {string.Join(", ", opponents.Select(o => o.Username))}");

            if (teammates.Count == 0)
            {
                Console.WriteLine("[ORDER ERROR] No teammates found!");
                return players;
            }

            if (opponents.Count < 2)
            {
                Console.WriteLine($"[ORDER ERROR] Not enough opponents found! Count: {opponents.Count}");
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

        public string GetAvatarIdForPlayer(string username)
        {
            try
            {
                if (username.StartsWith("Guest_"))
                {
                    return "avatar_aaa_default";
                }

                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.username == username);
                    if (user != null)
                    {
                        var profile = context.UserProfile.FirstOrDefault(up => up.userID == user.userID);
                        return profile?.avatarID ?? "avatar_aaa_default";
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetAvatarIdForPlayer));
            }

            return "avatar_aaa_default";
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