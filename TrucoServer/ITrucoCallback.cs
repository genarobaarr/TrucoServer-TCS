using System.Collections.Generic;
using System.ServiceModel;

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
        
        [OperationContract(IsOneWay = true)]
        void ReceiveCards(TrucoCard[] hand);

        [OperationContract(IsOneWay = true)]
        void NotifyCardPlayed(string playerName, string cardFileName, bool isLastCardOfRound);

        [OperationContract(IsOneWay = true)]
        void NotifyTurnChange(string nextPlayerName);

        [OperationContract(IsOneWay = true)]
        void NotifyScoreUpdate(int team1Score, int team2Score);

        [OperationContract(IsOneWay = true)]
        void NotifyTrucoCall(string callerName, string betName, bool needsResponse);

        [OperationContract(IsOneWay = true)]
        void NotifyResponse(string responderName, string response, string newBetState);

        [OperationContract(IsOneWay = true)]
        void NotifyRoundEnd(string winnerName, int team1Score, int team2Score);

        [OperationContract(IsOneWay = true)]
        void NotifyEnvidoCall(string callerName, string betName, int totalPoints, bool needsResponse);

        [OperationContract(IsOneWay = true)]
        void NotifyEnvidoResult(string winnerName, int score, int totalPointsAwarded);

        [OperationContract(IsOneWay = true)]
        void NotifyFlorCall(string callerName, string betName, int currentPoints, bool needsResponse);
    }
}
