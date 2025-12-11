using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;

namespace TrucoServer.Contracts
{
    /// <summary>
    /// Callback interface that the client must implement to receive asynchronous notifications from the server.
    /// </summary>
    [ServiceContract]
    public interface ITrucoCallback
    {
        /// <summary>
        /// Notifies that a player has joined the lobby.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The name of the player who joined.</param>
        [OperationContract(IsOneWay = true)]
        void OnPlayerJoined(string matchCode, string player);

        /// <summary>
        /// Notifies that a player has left the lobby.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The name of the player who left.</param>
        [OperationContract(IsOneWay = true)]
        void OnPlayerLeft(string matchCode, string player);

        /// <summary>
        /// Notifies that a card has been played.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The player who played the card.</param>
        /// <param name="card">The identifier of the card.</param>
        [OperationContract(IsOneWay = true)]
        void OnCardPlayed(string matchCode, string player, string card);

        /// <summary>
        /// Receives a chat message sent by another player.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The sender of the message.</param>
        /// <param name="message">The content of the message.</param>
        [OperationContract(IsOneWay = true)]
        void OnChatMessage(string matchCode, string player, string message);

        /// <summary>
        /// Notifies that the match has started and provides the list of participants.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="players">The list of players in the match.</param>
        [OperationContract(IsOneWay = true)]
        void OnMatchStarted(string matchCode, List<PlayerInfo> players);

        /// <summary>
        /// Notifies that the match has ended and announces the winner.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="winner">The name of the winning team or player.</param>
        [OperationContract(IsOneWay = true)]
        void OnMatchEnded(string matchCode, string winner);

        /// <summary>
        /// Notifies the client that a friend request has been received.
        /// </summary>
        /// <param name="fromUser">The username who sent the request.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendRequestReceived(string fromUser);

        /// <summary>
        /// Notifies the client that their friend request has been accepted.
        /// </summary>
        /// <param name="fromUser">The username who accepted the request.</param>
        [OperationContract(IsOneWay = true)]
        void OnFriendRequestAccepted(string fromUser);

        /// <summary>
        /// Delivers the cards for the current hand to the player.
        /// </summary>
        /// <param name="hand">An array of cards assigned to the player.</param>
        [OperationContract(IsOneWay = true)]
        void ReceiveCards(TrucoCard[] hand);

        /// <summary>
        /// Notifies all clients which card was played by a specific player.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="cardFileName">The visual identifier of the card.</param>
        /// <param name="isLastCardOfRound">Indicates if this card ends the current round.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyCardPlayed(string playerName, string cardFileName, bool isLastCardOfRound);

        /// <summary>
        /// Notifies the turn change to the next player.
        /// </summary>
        /// <param name="nextPlayerName">The name of the player who has the turn.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyTurnChange(string nextPlayerName);

        /// <summary>
        /// Updates the scores for both teams on the clients.
        /// </summary>
        /// <param name="team1Score">The score of Team 1.</param>
        /// <param name="team2Score">The score of Team 2.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyScoreUpdate(int team1Score, int team2Score);

        /// <summary>
        /// Notifies that a Truco call has been made.
        /// </summary>
        /// <param name="callerName">The player who called.</param>
        /// <param name="betName">The type of call (Truco, Retruco, etc.).</param>
        /// <param name="needsResponse">Indicates if the receiving client must respond.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyTrucoCall(string callerName, string betName, bool needsResponse);

        /// <summary>
        /// Notifies a player's response to a call (Quiero/No Quiero).
        /// </summary>
        /// <param name="responderName">The name of the player responding.</param>
        /// <param name="response">The response given.</param>
        /// <param name="newBetState">The new state of the bet.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyResponse(string responderName, string response, string newBetState);

        /// <summary>
        /// Notifies the end of a round and the updated scores.
        /// </summary>
        /// <param name="winnerName">The name of the round winner.</param>
        /// <param name="team1Score">The score of Team 1.</param>
        /// <param name="team2Score">The score of Team 2.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyRoundEnd(string winnerName, int team1Score, int team2Score);

        /// <summary>
        /// Notifies that an Envido call has been made.
        /// </summary>
        /// <param name="callerName">The player who called.</param>
        /// <param name="betName">The type of Envido.</param>
        /// <param name="needsResponse">Indicates if the receiving client must respond.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyEnvidoCall(string callerName, string betName, bool needsResponse);

        /// <summary>
        /// Notifies the final result of an Envido and the points awarded.
        /// </summary>
        /// <param name="winnerName">The name of the Envido winner.</param>
        /// <param name="score">The Envido score of the winner.</param>
        /// <param name="totalPointsAwarded">Total points awarded for the Envido.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyEnvidoResult(string winnerName, int score, int totalPointsAwarded);

        /// <summary>
        /// Notifies that Flor has been called.
        /// </summary>
        /// <param name="callerName">The player who called Flor.</param>
        /// <param name="betName">The name of the call.</param>
        /// <param name="needsResponse">Indicates if a response is required.</param>
        [OperationContract(IsOneWay = true)]
        void NotifyFlorCall(string callerName, string betName, bool needsResponse);

        /// <summary>
        /// Notifies the client that they have been forcibly disconnected.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void OnForcedLogout();

        /// <summary>
        /// Method to check connection.
        /// </summary>
        [OperationContract]
        void Ping();
    }
}
