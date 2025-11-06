using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    [ServiceContract]
    public interface ITrucoCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPlayerJoined(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void OnPlayerLeft(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void OnCardPlayed(string matchCode, string player, string card);

        [OperationContract(IsOneWay = true)]
        void OnChatMessage(string matchCode, string player, string message);

        [OperationContract(IsOneWay = true)]
        void OnMatchStarted(string matchCode, List<PlayerInfo> players);

        [OperationContract(IsOneWay = true)]
        void OnMatchEnded(string matchCode, string winner);

        [OperationContract(IsOneWay = true)]
        void OnFriendRequestReceived(string fromUser);

        [OperationContract(IsOneWay = true)]
        void OnFriendRequestAccepted(string fromUser);
    }
}
