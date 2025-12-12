using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Match
{
    public class LobbyRepository : ILobbyRepository
    {
        private const string STATUS_EXPIRED = "Expired";
        private const string STATUS_PENDING = "Pending";
        private const string STATUS_PUBLIC = "Public";
        private const string STATUS_PRIVATE = "Private";
        private const string STATUS_CLOSED = "Closed";
        private const string TEAM_1 = "Team 1";
        private const string TEAM_2 = "Team 2";
        private const string MATCH_1V1 = "1v1";
        private const string MATCH_2V2 = "2v2";
        private const string ROLE_OWNER = "Owner";
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";
        private const string INVALID_OPERATION_LOBBY_OR_HOST_NULL = "Lobby or Host cannot be null";
        private const string INVALID_OPERATION_HOST_NULL = "Host cannot be null";
        private const int MAX_PLAYERS_1V1 = 2;
        private const int VERSION_ID_IS_ZERO = 0;

        private readonly baseDatosTrucoEntities context;

        public LobbyRepository(baseDatosTrucoEntities context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public int ResolveVersionId(int maxPlayers)
        {
            try
            {
                int versionId = context.Versions
                    .Where(v => v.configuration.Contains(maxPlayers == MAX_PLAYERS_1V1 ? MATCH_1V1 : MATCH_2V2))
                    .Select(v => v.versionID)
                    .FirstOrDefault();

                return versionId == VERSION_ID_IS_ZERO && context.Versions.Any()
                    ? context.Versions.First().versionID
                    : versionId;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(ResolveVersionId));
               
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(ResolveVersionId));
                
                return 0;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ResolveVersionId));
                
                return 0;
            }
        }

        public Lobby CreateNewLobby(LobbyCreationOptions options)
        {
            try
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }
                
                if (options.Host == null)
                {
                    throw new InvalidOperationException(INVALID_OPERATION_HOST_NULL);
                }

                var newLobby = new Lobby
                {
                    ownerID = options.Host.userID,
                    versionID = options.VersionId,
                    maxPlayers = options.MaxPlayers,
                    status = options.Status,
                    createdAt = DateTime.Now
                };

                context.Lobby.Add(newLobby);
                context.SaveChanges();
                
                return newLobby;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(CreateNewLobby));
                
                return null;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(CreateNewLobby));
                
                return null;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(CreateNewLobby));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CreateNewLobby));
                
                return null;
            }
        }

        public void AddLobbyOwner(Lobby lobby, User host)
        {
            try
            {
                if (lobby == null || host == null)
                {
                    throw new InvalidOperationException(INVALID_OPERATION_LOBBY_OR_HOST_NULL);
                }

                context.LobbyMember.Add(new LobbyMember
                {
                    lobbyID = lobby.lobbyID,
                    userID = host.userID,
                    role = ROLE_OWNER,
                    team = TEAM_1
                });
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(AddLobbyOwner));
                throw;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(AddLobbyOwner));
                throw;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(AddLobbyOwner));
                throw;
            }
        }

        public Lobby ResolveLobbyForJoin(string matchCode)
        {
            try
            {
                int numericCode = new MatchCodeGenerator().GenerateNumericCodeFromString(matchCode);

                var invitation = context.Invitation.FirstOrDefault(i =>
                    i.code == numericCode &&
                    i.status == STATUS_PENDING &&
                    i.expiresAt > DateTime.Now);

                if (invitation == null)
                {
                    return null;
                }

                var lobbyCandidate = context.Lobby.FirstOrDefault(l =>
                    l.ownerID == invitation.senderID &&
                    !l.status.Equals(STATUS_CLOSED));

                return lobbyCandidate;
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(ResolveLobbyForJoin));
               
                return null;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(ResolveLobbyForJoin));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ResolveLobbyForJoin));
               
                return null;
            }
        }

        public LobbyLeaveResult ResolveLobbyForLeave(LobbyLeaveCriteria criteria)
        {
            try
            {
                if (criteria == null)
                {
                    throw new ArgumentNullException(nameof(criteria));
                }

                var player = context.User.FirstOrDefault(u => u.username == criteria.Username);

                if (player == null)
                {
                    return null;
                }

                var lobby = FindLobbyByMatchCode(criteria.MatchCode, true);

                return new LobbyLeaveResult
                {
                    Lobby = lobby,
                    Player = player
                };
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(ResolveLobbyForLeave));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ResolveLobbyForLeave));
                
                return null;
            }
        }

        public Lobby FindLobbyByMatchCode(string matchCode, bool onlyOpen = true)
        {
            try
            {
                int numericCode = new MatchCodeGenerator().GenerateNumericCodeFromString(matchCode);
                Lobby lobby = GetLobbyByMapping(matchCode, onlyOpen);

                if (lobby != null)
                {
                    return lobby;
                }

                var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);

                if (invitation == null)
                {
                    return null;
                }

                return GetLobbyByOwner(invitation.senderID, onlyOpen);
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(FindLobbyByMatchCode));
                
                return null;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(FindLobbyByMatchCode));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(FindLobbyByMatchCode));
                
                return null;
            }
        }

        private Lobby GetLobbyByMapping(string matchCode, bool onlyOpen)
        {
            try
            {
                int numericCode = new MatchCodeGenerator().GenerateNumericCodeFromString(matchCode);
                var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);

                if (invitation == null)
                {
                    return null;
                }

                return GetLobbyByOwner(invitation.senderID, onlyOpen);
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyByMapping));
                
                return null;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyByMapping));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyByMapping));
                
                return null;
            }
        }

        private Lobby GetLobbyByOwner(int ownerId, bool onlyOpen)
        {
            try
            {
                var query = context.Lobby.Where(l => l.ownerID == ownerId);

                if (onlyOpen)
                {
                    query = query.Where(l => l.status == STATUS_PUBLIC || l.status == STATUS_PRIVATE);
                }

                return query.FirstOrDefault();
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyByOwner));
               
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyByOwner));
                
                return null;
            }
        }

        public List<PlayerInformation> GetDatabasePlayers(Lobby lobby, string ownerUsername)
        {
            try
            {
                if (lobby == null)
                {
                    return new List<PlayerInformation>();
                }

                return (from lm in context.LobbyMember
                        join u in context.User on lm.userID equals u.userID
                        join up in context.UserProfile on u.userID equals up.userID into upj
                        from up in upj.DefaultIfEmpty()
                        where lm.lobbyID == lobby.lobbyID
                        select new PlayerInformation
                        {
                            Username = u.username,
                            AvatarId = up != null ? up.avatarID : DEFAULT_AVATAR_ID,
                            OwnerUsername = ownerUsername,
                            Team = lm.team
                        }).ToList();
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetDatabasePlayers));
                
                return new List<PlayerInformation>();
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(GetDatabasePlayers));
                
                return new List<PlayerInformation>();
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetDatabasePlayers));
               
                return new List<PlayerInformation>();
            }
        }

        public string GetLobbyOwnerName(int ownerId)
        {
            try
            {
                return context.User.Where(u => u.userID == ownerId).Select(u => u.username).FirstOrDefault();
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyOwnerName));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyOwnerName));
                
                return null;
            }
        }

        public bool CloseLobbyById(int lobbyId)
        {
            try
            {
                var lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == lobbyId);
                
                if (lobby == null)
                {
                    return false;
                }

                if (lobby.status != STATUS_CLOSED)
                {
                    lobby.status = STATUS_CLOSED;
                    context.SaveChanges();
                }
               
                return true;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(CloseLobbyById));
                
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(CloseLobbyById));
                
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CloseLobbyById));
                
                return false;
            }
        }

        public bool ExpireInvitationByMatchCode(string matchCode)
        {
            try
            {
                int numericCode = new MatchCodeGenerator().GenerateNumericCodeFromString(matchCode);

                var invitations = context.Invitation.Where(i => i.code == numericCode && i.status == STATUS_PENDING).ToList();

                if (!invitations.Any())
                {
                    return true;
                }

                foreach (var inv in invitations)
                {
                    inv.status = STATUS_EXPIRED;
                    inv.expiresAt = DateTime.Now;
                }

                context.SaveChanges();
                
                return true;
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitationByMatchCode));
                
                return false;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitationByMatchCode));
                
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitationByMatchCode));
                
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(ExpireInvitationByMatchCode));
               
                return false;
            }
        }

        public bool RemoveLobbyMembersById(int lobbyId)
        {
            try
            {
                var members = context.LobbyMember.Where(lm => lm.lobbyID == lobbyId).ToList();

                if (!members.Any())
                {
                    return true;
                }

                context.LobbyMember.RemoveRange(members);
                context.SaveChanges();

                return true;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(RemoveLobbyMembersById));
               
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(RemoveLobbyMembersById));
                
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(RemoveLobbyMembersById));
               
                return false;
            }
        }

        public bool IsPlayerInLobby(int lobbyId, int userId)
        {
            return context.LobbyMember.Any(lm => lm.lobbyID == lobbyId && lm.userID == userId);
        }

        public TeamCountsResult GetTeamCounts(int lobbyId)
        {
            int t1 = context.LobbyMember.Count(lm => lm.lobbyID == lobbyId && lm.team == TEAM_1);
            int t2 = context.LobbyMember.Count(lm => lm.lobbyID == lobbyId && lm.team == TEAM_2);

            return new TeamCountsResult 
            {
                Team1Count = t1, 
                Team2Count = t2
            };
        }

        public void AddMember(LobbyMemberDetails memberDetails)
        {
            context.LobbyMember.Add(new LobbyMember
            {
                lobbyID = memberDetails.LobbyId,
                userID = memberDetails.UserId,
                role = memberDetails.Role,
                team = memberDetails.Team
            });
            context.SaveChanges();
        }
    }
}
