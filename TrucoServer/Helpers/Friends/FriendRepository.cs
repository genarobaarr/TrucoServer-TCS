using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Friends
{
    public class FriendRepository : IFriendRepository
    {
        public bool GetUsersFromDatabase(baseDatosTrucoEntities context, string username1, string username2, out User user1, out User user2)
        {
            user1 = context.User.FirstOrDefault(u => u.username == username1);
            user2 = context.User.FirstOrDefault(u => u.username == username2);

            return user1 != null && user2 != null;
        }

        public bool CheckFriendshipExists(baseDatosTrucoEntities context, int userId1, int userId2)
        {
            return context.Friendship.Any(f =>
                (f.userID == userId1 && f.friendID == userId2) ||
                (f.userID == userId2 && f.friendID == userId1));
        }

        public void RegisterFriendRequest(baseDatosTrucoEntities context, int requesterId, int targetId, string statusPending)
        {
            var newRequest = new Friendship
            {
                userID = requesterId,
                friendID = targetId,
                status = statusPending
            };

            context.Friendship.Add(newRequest);
            context.SaveChanges();
            Console.WriteLine($"[FRIEND] Request sent from ID {requesterId} to ID {targetId}");
        }

        public Friendship FindPendingFriendship(baseDatosTrucoEntities context, int requesterId, int acceptorId, string statusPending)
        {
            return context.Friendship.FirstOrDefault(f =>
                f.userID == requesterId &&
                f.friendID == acceptorId &&
                f.status == statusPending);
        }

        public void CommitFriendshipAcceptance(baseDatosTrucoEntities context, Friendship request, int requesterId, int acceptorId, string statusAccepted)
        {
            request.status = statusAccepted;

            var reciprocalFriendship = new Friendship
            {
                userID = acceptorId,
                friendID = requesterId,
                status = statusAccepted
            };

            context.Friendship.Add(reciprocalFriendship);
            context.SaveChanges();
            Console.WriteLine($"[FRIEND] Friendship accepted between ID {requesterId} and ID {acceptorId}");
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
            Console.WriteLine($"[FRIEND] Relationship removed between ID {userId1} and ID {userId2}");
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
