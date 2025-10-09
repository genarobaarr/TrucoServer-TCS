using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoUserService
    {
        [OperationContract]
        bool Register(string username, string password, string email);

        [OperationContract]
        bool Login(string username, string password);

        [OperationContract(IsOneWay = true)]
        void Logout(string username);

        [OperationContract]
        List<string> GetOnlinePlayers();

        [OperationContract]
        List<PlayerStats> GetGlobalRanking();

        [OperationContract]
        List<MatchResult> GetLastMatches(string username);
    }

    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoFriendService
    {
        [OperationContract]
        bool SendFriendRequest(string fromUser, string toUser);

        [OperationContract(IsOneWay = true)]
        void AcceptFriendRequest(string fromUser, string toUser);

        [OperationContract]
        List<string> GetFriends(string username);
    }

    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoMatchService
    {
        [OperationContract]
        string CreateMatch(string hostPlayer);

        [OperationContract]
        bool JoinMatch(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void LeaveMatch(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void PlayCard(string matchCode, string player, string card);

        [OperationContract(IsOneWay = true)]
        void SendChatMessage(string matchCode, string player, string message);
    }
}
