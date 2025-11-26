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
        private const string ACCEPTED_STATUS = "Accepted";
        private const string PENDING_STATUS = "Pending";

        public bool SendFriendRequest(string fromUser, string toUser)
        {
            try
            {
                using (var db = new baseDatosTrucoEntities())
                {
                    User requester = db.User.FirstOrDefault(u => u.username == fromUser);
                    User target = db.User.FirstOrDefault(u => u.username == toUser);

                    if (requester == null || target == null)
                    {
                        return false;
                    }

                    int requesterId = requester.userID;
                    int targetId = target.userID;

                    bool friendshipExists = db.Friendship.Any(f =>
                        (f.userID == requesterId && f.friendID == targetId) ||
                        (f.userID == targetId && f.friendID == requesterId));

                    if (friendshipExists)
                    {
                        return false;
                    }

                    Friendship newRequest = new Friendship
                    {
                        userID = requesterId,
                        friendID = targetId,
                        status = PENDING_STATUS
                    };

                    db.Friendship.Add(newRequest);
                    db.SaveChanges();

                    var targetUserCallback = TrucoUserServiceImp.GetUserCallback(toUser);
                    targetUserCallback?.OnFriendRequestReceived(fromUser);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - SQL Server Error");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendFriendRequest)} - Callback Communication Error");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SendFriendRequest));
                return false;
            }
        }
        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            try
            {
                using (var db = new baseDatosTrucoEntities())
                {
                    User requester = db.User.FirstOrDefault(u => u.username == fromUser);
                    User acceptor = db.User.FirstOrDefault(u => u.username == toUser);

                    if (requester == null || acceptor == null)
                    {
                        return false;
                    }

                    Friendship request = db.Friendship.FirstOrDefault(f =>
                        f.userID == requester.userID &&
                        f.friendID == acceptor.userID &&
                        f.status == PENDING_STATUS);

                    if (request == null)
                    {
                        return false;
                    }

                    request.status = ACCEPTED_STATUS; Friendship reciprocalFriendship = new Friendship
                    {
                        userID = acceptor.userID,
                        friendID = requester.userID,
                        status = ACCEPTED_STATUS
                    };

                    db.Friendship.Add(reciprocalFriendship); db.SaveChanges(); 
                    var fromUserCallback = TrucoUserServiceImp.GetUserCallback(fromUser);

                    fromUserCallback?.OnFriendRequestAccepted(toUser); 
                    
                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - SQL Server Error");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(AcceptFriendRequest)} - Callback Communication Error");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(AcceptFriendRequest));
                return false;
            }
        }
        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            try
            {
                using (var db = new baseDatosTrucoEntities())
                {
                    User u1 = db.User.FirstOrDefault(u => u.username == user1);
                    User u2 = db.User.FirstOrDefault(u => u.username == user2);

                    if (u1 == null || u2 == null)
                    {
                        return false;
                    }

                    List<Friendship> toRemove = db.Friendship.Where(f =>
                        (f.userID == u1.userID && f.friendID == u2.userID) ||
                        (f.userID == u2.userID && f.friendID == u1.userID)).ToList();

                    if (!toRemove.Any())
                    {
                        return false;
                    }

                    db.Friendship.RemoveRange(toRemove);
                    db.SaveChanges();
                    
                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveFriendOrRequest)} - DataBase Deletion Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(RemoveFriendOrRequest)} - SQL Server Error");
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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username.ToLower() == username.ToLower());

                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    int currentUserId = user.userID; 
                    
                    var friendsData = context.Friendship
                        .Where(f => (f.userID == currentUserId || f.friendID == currentUserId) && f.status == ACCEPTED_STATUS)
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

                    return friendsData;
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetFriends)} - SQL Server Error");
                return new List<FriendData>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetFriends)} - Invalid Operation (DataBase Context)");
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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username.ToLower() == username.ToLower());

                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    int currentUserId = user.userID; 
                    
                    var pendingRequests = context.Friendship
                        .Where(f => f.friendID == currentUserId && f.status == PENDING_STATUS)
                        .Join(context.User.Include("UserProfile"),
                              f => f.userID,
                              u => u.userID,
                              (f, u) => new FriendData
                              {
                                  Username = u.username,
                                  AvatarId = u.UserProfile.avatarID
                              })
                        .ToList(); 
                    
                    return pendingRequests;
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPendingFriendRequests)} - SQL Server Error");
                return new List<FriendData>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetPendingFriendRequests)} - Invalid Operation (DataBase Context)");
                return new List<FriendData>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetPendingFriendRequests));
                return new List<FriendData>();
            }
        }
    }
}
