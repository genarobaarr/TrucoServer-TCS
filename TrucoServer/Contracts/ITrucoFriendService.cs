using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    /// <summary>
    /// Service contract for managing friends and friendship requests.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoFriendService
    {
        /// <summary>
        /// Sends a friend request from one user to another.
        /// </summary>
        /// <param name="fromUser">The username sending the request.</param>
        /// <param name="toUser">The username receiving the request.</param>
        /// <returns>True if the request was sent successfully; otherwise, False.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool SendFriendRequest(string fromUser, string toUser);

        /// <summary>
        /// Accepts a pending friend request.
        /// </summary>
        /// <param name="fromUser">The username who sent the original request.</param>
        /// <param name="toUser">The username accepting the request.</param>
        /// <returns>True if the request was accepted successfully; otherwise, False.</returns>
        [OperationContract]
        bool AcceptFriendRequest(string fromUser, string toUser);

        /// <summary>
        /// Removes a friend or cancels/rejects a friend request.
        /// </summary>
        /// <param name="user1">The first user involved.</param>
        /// <param name="user2">The second user involved.</param>
        /// <returns>True if the relationship was removed; otherwise, False.</returns>
        [OperationContract]
        bool RemoveFriendOrRequest(string user1, string user2);

        /// <summary>
        /// Retrieves the list of accepted friends for a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>A list of friend data objects.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<FriendData> GetFriends(string username);

        /// <summary>
        /// Retrieves the list of pending friend requests received by the user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>A list of data objects for users who sent requests.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<FriendData> GetPendingFriendRequests(string username);
    }
}
