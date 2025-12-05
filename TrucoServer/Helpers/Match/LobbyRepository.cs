using System;
using System.Linq;
using System.Collections.Generic;
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
                    .Where(v => v.configuration.Contains(maxPlayers == 2 ? MATCH_1V1 : MATCH_2V2))
                    .Select(v => v.versionID)
                    .FirstOrDefault();

                return versionId == 0 && context.Versions.Any()
                    ? context.Versions.First().versionID
                    : versionId;
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
                    throw new ArgumentNullException(nameof(options.Host));
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
                context.LobbyMember.Add(new LobbyMember
                {
                    lobbyID = lobby.lobbyID,
                    userID = host.userID,
                    role = ROLE_OWNER,
                    team = TEAM_1
                });
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(AddLobbyOwner));
                throw;
            }
        }

        public void CreatePrivateInvitation(User host, string matchCode)
        {
            try
            {
                int numericCode = new MatchCodeGenerator().GenerateNumericCodeFromString(matchCode);
                var previousInvitations = context.Invitation
                    .Where(i => i.senderID == host.userID && i.status == STATUS_PENDING)
                    .ToList();

                foreach (var inv in previousInvitations)
                {
                    inv.status = STATUS_EXPIRED;
                    inv.expiresAt = DateTime.Now;
                }

                context.Invitation.Add(new Invitation
                {
                    senderID = host.userID,
                    receiverEmail = null,
                    code = numericCode,
                    status = STATUS_PENDING,
                    expiresAt = DateTime.Now.AddHours(2)
                });
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex,nameof(CreatePrivateInvitation));
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
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLobbyByOwner));
                return null;
            }
        }

        public List<PlayerInfo> GetDatabasePlayers(Lobby lobby, string ownerUsername)
        {
            try
            {
                return (from lm in context.LobbyMember
                        join u in context.User on lm.userID equals u.userID
                        join up in context.UserProfile on u.userID equals up.userID into upj
                        from up in upj.DefaultIfEmpty()
                        where lm.lobbyID == lobby.lobbyID
                        select new PlayerInfo
                        {
                            Username = u.username,
                            AvatarId = up != null ? up.avatarID : DEFAULT_AVATAR_ID,
                            OwnerUsername = ownerUsername,
                            Team = lm.team
                        }).ToList();
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetDatabasePlayers));
                return new List<PlayerInfo>();
            }
        }

        public string GetLobbyOwnerName(int ownerId)
        {
            return context.User.Where(u => u.userID == ownerId).Select(u => u.username).FirstOrDefault();
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

            return new TeamCountsResult { Team1Count = t1, Team2Count = t2 };
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
