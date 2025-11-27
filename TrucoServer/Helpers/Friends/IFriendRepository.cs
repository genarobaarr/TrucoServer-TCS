using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Friends
{
    public interface IFriendRepository
    {
        bool GetUsersFromDatabase(baseDatosTrucoEntities context, string username1, string username2, out User user1, out User user2);
        bool CheckFriendshipExists(baseDatosTrucoEntities context, int userId1, int userId2);
        void RegisterFriendRequest(baseDatosTrucoEntities context, int requesterId, int targetId, string statusPending);
        Friendship FindPendingFriendship(baseDatosTrucoEntities context, int requesterId, int acceptorId, string statusPending);
        void CommitFriendshipAcceptance(baseDatosTrucoEntities context, Friendship request, int requesterId, int acceptorId, string statusAccepted);
        bool DeleteFriendships(baseDatosTrucoEntities context, int userId1, int userId2);
        List<FriendData> QueryFriendsList(baseDatosTrucoEntities context, int currentUserId, string statusAccepted);
        List<FriendData> QueryPendingRequests(baseDatosTrucoEntities context, int currentUserId, string statusPending);
    }
}
