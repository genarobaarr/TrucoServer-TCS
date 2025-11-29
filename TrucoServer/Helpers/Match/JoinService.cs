using System;
using System.Linq;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Match
{
    public class JoinService : IJoinService
    {
        private const string GUEST_PREFIX = "Guest_";
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";
        private const string ROLE_PLAYER = "Player";
        private const string STATUS_CLOSED = "Closed";
        private const string STATUS_PUBLIC = "Public";

        private readonly ILobbyCoordinator coordinator;

        public JoinService(ILobbyCoordinator coordinator)
        {
            this.coordinator = coordinator;
        }

        public bool ProcessSafeJoin(int lobbyId, string matchCode, string player)
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

        public bool TryJoinAsGuest(baseDatosTrucoEntities context, Lobby lobby, string matchCode, string player)
        {
            if (!lobby.status.Equals(STATUS_PUBLIC, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[JOIN GUEST] Denied: Lobby {lobby.lobbyID} is not public");
                return false;
            }

            int currentDbPlayers = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
            int guestCount = coordinator.GetGuestCountInMemory(matchCode);
            int totalPlayers = currentDbPlayers + guestCount;

            if (totalPlayers >= lobby.maxPlayers)
            {
                Console.WriteLine($"[JOIN GUEST] Denied: Public lobby {lobby.lobbyID} is full ({totalPlayers}/{lobby.maxPlayers})");
                return false;
            }

            Console.WriteLine($"[JOIN GUEST] Approved: Guest {player} joining lobby {lobby.lobbyID} ({totalPlayers + 1}/{lobby.maxPlayers})");
            return true;
        }

        public bool TryJoinAsUser(baseDatosTrucoEntities context, Lobby lobby, string player)
        {
            User playerUser = context.User.FirstOrDefault(u => u.username == player);

            if (!ValidateJoinConditions(context, lobby, playerUser))
            {
                return false;
            }

            AddPlayerToLobby(context, lobby, playerUser);
            return true;
        }

        public bool ValidateJoinConditions(baseDatosTrucoEntities context, Lobby lobby, User playerUser)
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

        public void AddPlayerToLobby(baseDatosTrucoEntities context, Lobby lobby, User playerUser)
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

        public string DetermineTeamForNewPlayer(int maxPlayers, int team1Count, int team2Count, string username)
        {
            if (maxPlayers == 2)
            {
                if (team1Count == 0 && team2Count == 0)
                {
                    return TEAM_1;
                }

                return TEAM_2;
            }

            return (team1Count > team2Count) ? TEAM_2 : TEAM_1;
        }

        public bool SwitchGuestTeam(string matchCode, string username)
        {
            if (coordinator.TryGetCallbacksSnapshot(matchCode, out var _))
            {
                var guestInfo = coordinator.TryGetCallbacksSnapshot(matchCode, out var snapshot)
                    ? snapshot.Select(cb => coordinator.GetPlayerInfoFromCallback(cb)).FirstOrDefault(info => info != null && info.Username == username)
                    : null;

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

        public bool SwitchUserTeam(string matchCode, string username)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = new LobbyRepository().FindLobbyByMatchCode(context, matchCode, true);
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

        public bool CanJoinTeam(string matchCode, string targetTeam)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var lobby = new LobbyRepository().FindLobbyByMatchCode(context, matchCode, true);

                if (lobby == null)
                {
                    return false;
                }

                int dbCount = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID && lm.team == targetTeam);
                int memCount = coordinator.GetGuestCountInMemory(matchCode);

                if (coordinator.TryGetCallbacksSnapshot(matchCode, out var snapshot))
                {
                    memCount = snapshot.Select(cb => coordinator.GetPlayerInfoFromCallback(cb)).Count(info => info != null && info.Username.StartsWith(GUEST_PREFIX) && info.Team == targetTeam);
                }

                return (dbCount + memCount) < (lobby.maxPlayers / 2);
            }
        }
    }
}
