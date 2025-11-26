using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Services
{
    public class TrucoFriendServiceImp : ITrucoFriendService
    {
        private const string STATUS_ACCEPTED = "Accepted";
        private const string STATUS_PENDING = "Pending";

        public bool SendFriendRequest(string fromUser, string toUser)
        {
            if (!ServerValidator.IsUsernameValid(fromUser) ||
                !ServerValidator.IsUsernameValid(toUser) ||
                fromUser.Equals(toUser, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (!GetUsersFromDatabase(context, fromUser, toUser, out User requester, out User target))
                    {
                        return false;
                    }

                    if (CheckFriendshipExists(context, requester.userID, target.userID))
                    {
                        return false;
                    }

                    RegisterFriendRequest(context, requester.userID, target.userID);
                    NotifyRequestReceived(toUser, fromUser);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - Database Update Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - SQL Server Error");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - WCF Communication Error");
                return true;
            }
            catch (TimeoutException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - Timeout Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SendFriendRequest));
                return false;
            }
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            if (!ServerValidator.IsUsernameValid(fromUser) || !ServerValidator.IsUsernameValid(toUser))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (!GetUsersFromDatabase(context, fromUser, toUser, out User requester, out User acceptor))
                    {
                        return false;
                    }

                    var request = FindPendingFriendship(context, requester.userID, acceptor.userID);
                    if (request == null)
                    {
                        return false;
                    }

                    CommitFriendshipAcceptance(context, request, requester.userID, acceptor.userID);
                    NotifyRequestAccepted(fromUser, toUser);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - Database Update Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - SQL Server Error");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - WCF Communication Error");
                return true;
            }
            catch (TimeoutException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - Timeout Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AcceptFriendRequest));
                return false;
            }
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            if (!ServerValidator.IsUsernameValid(user1) || !ServerValidator.IsUsernameValid(user2))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (!GetUsersFromDatabase(context, user1, user2, out User u1, out User u2))
                    {
                        return false;
                    }

                    return DeleteFriendships(context, u1.userID, u2.userID);
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveFriendOrRequest)} - Database Deletion Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveFriendOrRequest)} - SQL Server Error");
                return false;
            }
            catch (TimeoutException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveFriendOrRequest)} - Timeout Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RemoveFriendOrRequest));
                return false;
            }
        }

        public List<FriendData> GetFriends(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return new List<FriendData>();
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username == username);
                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    return QueryFriendsList(context, user.userID);
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetFriends)} - SQL Server Error");
                return new List<FriendData>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetFriends)} - Invalid Operation (DB Context)");
                return new List<FriendData>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetFriends));
                return new List<FriendData>();
            }
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return new List<FriendData>();
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username == username);
                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    return QueryPendingRequests(context, user.userID);
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPendingFriendRequests)} - SQL Server Error");
                return new List<FriendData>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPendingFriendRequests)} - Invalid Operation (DB Context)");
                return new List<FriendData>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPendingFriendRequests));
                return new List<FriendData>();
            }
        }

        private bool GetUsersFromDatabase(baseDatosTrucoEntities context, string username1, string username2, out User user1, out User user2)
        {
            user1 = context.User.FirstOrDefault(u => u.username == username1);
            user2 = context.User.FirstOrDefault(u => u.username == username2);

            return user1 != null && user2 != null;
        }

        private bool CheckFriendshipExists(baseDatosTrucoEntities context, int userId1, int userId2)
        {
            return context.Friendship.Any(f =>
                (f.userID == userId1 && f.friendID == userId2) ||
                (f.userID == userId2 && f.friendID == userId1));
        }

        private void RegisterFriendRequest(baseDatosTrucoEntities context, int requesterId, int targetId)
        {
            var newRequest = new Friendship
            {
                userID = requesterId,
                friendID = targetId,
                status = STATUS_PENDING
            };

            context.Friendship.Add(newRequest);
            context.SaveChanges();
            Console.WriteLine($"[FRIEND] Request sent from ID {requesterId} to ID {targetId}");
        }

        private Friendship FindPendingFriendship(baseDatosTrucoEntities context, int requesterId, int acceptorId)
        {
            return context.Friendship.FirstOrDefault(f =>
                f.userID == requesterId &&
                f.friendID == acceptorId &&
                f.status == STATUS_PENDING);
        }

        private void CommitFriendshipAcceptance(baseDatosTrucoEntities context, Friendship request, int requesterId, int acceptorId)
        {
            request.status = STATUS_ACCEPTED;

            var reciprocalFriendship = new Friendship
            {
                userID = acceptorId,
                friendID = requesterId,
                status = STATUS_ACCEPTED
            };

            context.Friendship.Add(reciprocalFriendship);
            context.SaveChanges();
            Console.WriteLine($"[FRIEND] Friendship accepted between ID {requesterId} and ID {acceptorId}");
        }

        private bool DeleteFriendships(baseDatosTrucoEntities context, int userId1, int userId2)
        {
            var toRemove = context.Friendship.Where(f =>
                (f.userID == userId1 && f.friendID == userId2) ||
                (f.userID == userId2 && f.friendID == userId1)).ToList();

            if (!toRemove.Any())
            {
                return false;
            }

            context.Friendship.RemoveRange(toRemove);
            context.SaveChanges();
            Console.WriteLine($"[FRIEND] Relationship removed between ID {userId1} and ID {userId2}");
            return true;
        }

        private List<FriendData> QueryFriendsList(baseDatosTrucoEntities context, int currentUserId)
        {
            return context.Friendship
                .Where(f => (f.userID == currentUserId || f.friendID == currentUserId) && f.status == STATUS_ACCEPTED)
                .Select(f => f.userID == currentUserId ? f.friendID : f.userID)
                .Distinct()
                .Join(context.User.Include("UserProfile"),
                      friendId => friendId,
                      u => u.userID,
                      (friendId, u) => new FriendData
                      {
                          Username = u.username,
                          AvatarId = u.UserProfile.avatarID
                      })
                .ToList();
        }

        private List<FriendData> QueryPendingRequests(baseDatosTrucoEntities context, int currentUserId)
        {
            return context.Friendship
                .Where(f => f.friendID == currentUserId && f.status == STATUS_PENDING)
                .Join(context.User.Include("UserProfile"),
                      f => f.userID,
                      u => u.userID,
                      (f, u) => new FriendData
                      {
                          Username = u.username,
                          AvatarId = u.UserProfile.avatarID
                      })
                .ToList();
        }

        private void NotifyRequestReceived(string targetUsername, string fromUsername)
        {
            try
            {
                var callback = TrucoUserServiceImp.GetUserCallback(targetUsername);
                if (callback != null)
                {
                    callback.OnFriendRequestReceived(fromUsername);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, $"{nameof(NotifyRequestReceived)} - Failed to notify {targetUsername}");
            }
        }

        private void NotifyRequestAccepted(string targetUsername, string fromUsername)
        {
            try
            {
                var callback = TrucoUserServiceImp.GetUserCallback(targetUsername);
                if (callback != null)
                {
                    callback.OnFriendRequestAccepted(fromUsername);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, $"{nameof(NotifyRequestAccepted)} - Failed to notify {targetUsername}");
            }
        }
    }
}