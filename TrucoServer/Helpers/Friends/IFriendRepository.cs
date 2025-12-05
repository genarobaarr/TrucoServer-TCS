using System.Collections.Generic;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Friends
{
    public interface IFriendRepository
    {
        UserLookupResult GetUsersFromDatabase(UserLookupOptions options);
        bool CheckFriendshipExists(int userId1, int userId2);
        void RegisterFriendRequest(FriendRequest request);
        Friendship FindPendingFriendship(FriendRequest criteria);
        void CommitFriendshipAcceptance(FriendshipCommitOptions options);
        bool DeleteFriendships(int userId1, int userId2);
        List<FriendData> QueryFriendsList(int currentUserId, string statusAccepted);
        List<FriendData> QueryPendingRequests(int currentUserId, string statusPending);
    }
}
