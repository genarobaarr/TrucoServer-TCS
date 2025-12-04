using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Friends
{
    public interface IFriendRepository
    {
        UserLookupResult GetUsersFromDatabase(baseDatosTrucoEntities context, UserLookupOptions options);
        bool CheckFriendshipExists(baseDatosTrucoEntities context, int userId1, int userId2);
        void RegisterFriendRequest(baseDatosTrucoEntities context, int requesterId, int targetId, string statusPending);
        Friendship FindPendingFriendship(baseDatosTrucoEntities context, int requesterId, int acceptorId, string statusPending);
        void CommitFriendshipAcceptance(baseDatosTrucoEntities context, FriendshipCommitOptions options);
        bool DeleteFriendships(baseDatosTrucoEntities context, int userId1, int userId2);
        List<FriendData> QueryFriendsList(baseDatosTrucoEntities context, int currentUserId, string statusAccepted);
        List<FriendData> QueryPendingRequests(baseDatosTrucoEntities context, int currentUserId, string statusPending);
    }
}
