using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoFriendService
    {
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool SendFriendRequest(string fromUser, string toUser);

        [OperationContract]
        bool AcceptFriendRequest(string fromUser, string toUser);

        [OperationContract]
        bool RemoveFriendOrRequest(string user1, string user2);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<FriendData> GetFriends(string username);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<FriendData> GetPendingFriendRequests(string username);
    }
}
