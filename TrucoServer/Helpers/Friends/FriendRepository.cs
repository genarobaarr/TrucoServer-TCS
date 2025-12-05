using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Friends
{
    public class FriendRepository : IFriendRepository
    {
        public UserLookupResult GetUsersFromDatabase(baseDatosTrucoEntities context, UserLookupOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var user1 = context.User.FirstOrDefault(u => u.username == options.Username1);
            var user2 = context.User.FirstOrDefault(u => u.username == options.Username2);

            return new UserLookupResult
            {
                User1 = user1,
                User2 = user2,
                Success = user1 != null && user2 != null
            };
        }

        public bool CheckFriendshipExists(baseDatosTrucoEntities context, int userId1, int userId2)
        {
            return context.Friendship.Any(f =>
                (f.userID == userId1 && f.friendID == userId2) ||
                (f.userID == userId2 && f.friendID == userId1));
        }

        public void RegisterFriendRequest(baseDatosTrucoEntities context, FriendRequest request)
        {
            var newRequest = new Friendship
            {
                userID = request.RequesterId,
                friendID = request.TargetId,
                status = request.Status
            };

            context.Friendship.Add(newRequest);
            context.SaveChanges();
        }

        public Friendship FindPendingFriendship(baseDatosTrucoEntities context, FriendRequest request)
        {
            return context.Friendship.FirstOrDefault(f =>
                f.userID == request.RequesterId &&
                f.friendID == request.TargetId &&
                f.status == request.Status);
        }

        public void CommitFriendshipAcceptance(baseDatosTrucoEntities context, FriendshipCommitOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Request == null)
            {
                throw new ArgumentNullException(nameof(options.Request));
            }

            options.Request.status = options.StatusAccepted;

            var reciprocalFriendship = new Friendship
            {
                userID = options.AcceptorId,
                friendID = options.RequesterId,
                status = options.StatusAccepted
            };

            context.Friendship.Add(reciprocalFriendship);
            context.SaveChanges();
        }

        public bool DeleteFriendships(baseDatosTrucoEntities context, int userId1, int userId2)
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
            return true;
        }

        public List<FriendData> QueryFriendsList(baseDatosTrucoEntities context, int currentUserId, string statusAccepted)
        {
            return context.Friendship
                .Where(f => (f.userID == currentUserId || f.friendID == currentUserId) && f.status == statusAccepted)
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

        public List<FriendData> QueryPendingRequests(baseDatosTrucoEntities context, int currentUserId, string statusPending)
        {
            return context.Friendship
                .Where(f => f.friendID == currentUserId && f.status == statusPending)
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
    }
}
