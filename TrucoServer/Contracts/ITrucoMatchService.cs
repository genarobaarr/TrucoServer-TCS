using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoMatchService
    {
        [OperationContract]
        string CreateLobby(string hostUsername, int maxPlayers, string privacy);

        [OperationContract]
        bool JoinMatch(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void LeaveMatch(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void JoinMatchChat(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void LeaveMatchChat(string matchCode, string player);

        [OperationContract(IsOneWay = true)]
        void SendChatMessage(string matchCode, string player, string message);

        [OperationContract]
        List<PlayerInfo> GetLobbyPlayers(string matchCode);

        [OperationContract]
        List<PublicLobbyInfo> GetPublicLobbies();

        [OperationContract]
        void SwitchTeam(string matchCode, string username);

        [OperationContract(IsOneWay = true)]
        void StartMatch(string matchCode);

        [OperationContract(IsOneWay = true)]
        void PlayCard(string matchCode, string cardFileName);

        [OperationContract(IsOneWay = true)]
        void CallTruco(string matchCode, string betType);

        [OperationContract(IsOneWay = true)]
        void RespondToCall(string matchCode, string response);

        [OperationContract(IsOneWay = true)]
        void CallEnvido(string matchCode, string betType);

        [OperationContract(IsOneWay = true)]
        void RespondToEnvido(string matchCode, string response);

        [OperationContract(IsOneWay = true)]
        void GoToDeck(string matchCode);

        [OperationContract(IsOneWay = true)]
        void CallFlor(string matchCode, string betType);

        [OperationContract(IsOneWay = true)]
        void RespondToFlor(string matchCode, string response);
    }
}
