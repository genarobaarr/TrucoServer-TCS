using System;
using System.Linq;
using TrucoServer.Data.DTOs;
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
                    return false;
                }

                bool isGuest = player.StartsWith(GUEST_PREFIX);

                if (isGuest)
                {
                    var guestOptions = new GuestJoinOptions
                    {
                        Lobby = freshLobby,
                        MatchCode = matchCode,
                        PlayerUsername = player
                    };
                    return TryJoinAsGuest(context, guestOptions);
                }
                else
                {
                    return TryJoinAsUser(context, freshLobby, player);
                }
            }
        }

        public bool TryJoinAsGuest(baseDatosTrucoEntities context, GuestJoinOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Lobby == null)
            {
                throw new ArgumentNullException(nameof(options.Lobby));
            }

            if (!options.Lobby.status.Equals(STATUS_PUBLIC, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int currentDbPlayers = context.LobbyMember.Count(lm => lm.lobbyID == options.Lobby.lobbyID);

            int guestCount = coordinator.GetGuestCountInMemory(options.MatchCode);
            int totalPlayers = currentDbPlayers + guestCount;

            if (totalPlayers >= options.Lobby.maxPlayers)
            {
                return false;
            }

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
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ValidateJoinConditions));
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

                var teamOptions = new TeamDeterminationOptions
                {
                    MaxPlayers = lobby.maxPlayers,
                    Team1Count = team1Count,
                    Team2Count = team2Count,
                    Username = playerUser.username
                };

                string assignedTeam = DetermineTeamForNewPlayer(teamOptions);

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
                ServerException.HandleException(ex, nameof(AddPlayerToLobby));
                throw;
            }
        }

        public string DetermineTeamForNewPlayer(TeamDeterminationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.MaxPlayers == 2)
            {
                if (options.Team1Count == 0 && options.Team2Count == 0)
                {
                    return TEAM_1;
                }

                return TEAM_2;
            }

            return (options.Team1Count > options.Team2Count) ? TEAM_2 : TEAM_1;
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
