using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Friends
{
    public interface IFriendRepository
    {
        UserLookupResult GetUsersFromDatabase(baseDatosTrucoEntities context, UserLookupOptions options);
        bool CheckFriendshipExists(baseDatosTrucoEntities context, int userId1, int userId2);
        void RegisterFriendRequest(baseDatosTrucoEntities context, FriendRequest request);
        Friendship FindPendingFriendship(baseDatosTrucoEntities context, FriendRequest criteria);
        void CommitFriendshipAcceptance(baseDatosTrucoEntities context, FriendshipCommitOptions options);
        bool DeleteFriendships(baseDatosTrucoEntities context, int userId1, int userId2);
        List<FriendData> QueryFriendsList(baseDatosTrucoEntities context, int currentUserId, string statusAccepted);
        List<FriendData> QueryPendingRequests(baseDatosTrucoEntities context, int currentUserId, string statusPending);
    }
}
