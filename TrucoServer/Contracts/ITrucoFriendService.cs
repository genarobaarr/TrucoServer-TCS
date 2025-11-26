using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoFriendService
    {
        [OperationContract]
        bool SendFriendRequest(string fromUser, string toUser);

        [OperationContract]
        bool AcceptFriendRequest(string fromUser, string toUser);

        [OperationContract]
        bool RemoveFriendOrRequest(string user1, string user2);

        [OperationContract]
        List<FriendData> GetFriends(string username);

        [OperationContract]
        List<FriendData> GetPendingFriendRequests(string username);
    }
}
