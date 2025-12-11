using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    /// <summary>
    /// Service contract for managing matches, lobbies, and Truco game logic.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoMatchService
    {
        /// <summary>
        /// Creates a new game lobby.
        /// </summary>
        /// <param name="hostUsername">The username of the host.</param>
        /// <param name="maxPlayers">The maximum number of players (2 or 4).</param>
        /// <param name="privacy">The privacy level ("Public" or "Private").</param>
        /// <returns>The generated match code.</returns>
        [OperationContract]
        string CreateLobby(string hostUsername, int maxPlayers, string privacy);

        /// <summary>
        /// Allows a player to join an existing match.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The username of the player joining.</param>
        /// <returns>The maximum number of players in the lobby if successful; otherwise, 0.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        int JoinMatch(string matchCode, string player);

        /// <summary>
        /// Allows a player to leave a match.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The username of the player leaving.</param>
        [OperationContract(IsOneWay = true)]
        void LeaveMatch(string matchCode, string player);

        /// <summary>
        /// Registers a player to the match chat to receive messages.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The username of the player.</param>
        [OperationContract(IsOneWay = true)]
        void JoinMatchChat(string matchCode, string player);

        /// <summary>
        /// Unregisters a player from the match chat.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The username of the player.</param>
        [OperationContract(IsOneWay = true)]
        void LeaveMatchChat(string matchCode, string player);

        /// <summary>
        /// Sends a chat message to all players in the match.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="player">The username sending the message.</param>
        /// <param name="message">The content of the message.</param>
        [OperationContract(IsOneWay = true)]
        void SendChatMessage(string matchCode, string player, string message);

        /// <summary>
        /// Retrieves the list of players currently in a lobby.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <returns>A list of player information objects.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<PlayerInfo> GetLobbyPlayers(string matchCode);

        /// <summary>
        /// Retrieves a list of available public lobbies.
        /// </summary>
        /// <returns>A list containing information about public lobbies.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<PublicLobbyInfo> GetPublicLobbies();

        /// <summary>
        /// Allows a player to switch teams within the lobby.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="username">The username of the player.</param>
        [OperationContract]
        void SwitchTeam(string matchCode, string username);

        /// <summary>
        /// Retrieves the list of banned words for the chat filter.
        /// </summary>
        /// <returns>An object containing the list of banned words.</returns>
        [OperationContract]
        BannedWordList GetBannedWords();

        /// <summary>
        /// Sends an invitation to a friend to join a match.
        /// </summary>
        /// <param name="inviteContext">An object containing invitation details (code, sender, receiver).</param>
        /// <returns>True if the invitation was sent successfully; otherwise, False.</returns>
        [OperationContract]
        bool InviteFriend(InviteFriendOptions inviteContext);

        /// <summary>
        /// Starts the match once all players are ready.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        [OperationContract(IsOneWay = true)]
        void StartMatch(string matchCode);

        /// <summary>
        /// Executes a card play action by a player.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="cardFileName">The file name or identifier of the card played.</param>
        [OperationContract(IsOneWay = true)]
        void PlayCard(string matchCode, string cardFileName);

        /// <summary>
        /// Calls a Truco bet (Truco, Retruco, Vale Cuatro).
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="betType">The type of bet made.</param>
        [OperationContract(IsOneWay = true)]
        void CallTruco(string matchCode, string betType);

        /// <summary>
        /// Responds to a Truco bet (Quiero, No Quiero).
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="response">The player's response.</param>
        [OperationContract(IsOneWay = true)]
        void RespondToCall(string matchCode, string response);

        /// <summary>
        /// Calls an Envido bet (Envido, Real Envido, Falta Envido).
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="betType">The type of Envido call.</param>
        [OperationContract(IsOneWay = true)]
        void CallEnvido(string matchCode, string betType);

        /// <summary>
        /// Responds to an Envido call.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="response">The player's response.</param>
        [OperationContract(IsOneWay = true)]
        void RespondToEnvido(string matchCode, string response);

        /// <summary>
        /// A player decides to go to the deck (fold the current hand).
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        [OperationContract(IsOneWay = true)]
        void GoToDeck(string matchCode);

        /// <summary>
        /// Calls a Flor.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="betType">The type of Flor call.</param>
        [OperationContract(IsOneWay = true)]
        void CallFlor(string matchCode, string betType);

        /// <summary>
        /// Responds to a Flor call or challenge.
        /// </summary>
        /// <param name="matchCode">The unique code of the match.</param>
        /// <param name="response">The player's response.</param>
        [OperationContract(IsOneWay = true)]
        void RespondToFlor(string matchCode, string response);

        /// <summary>
        /// Reports activy for current turn user.
        /// </summary>
        /// <param name="currentTurnPlayerName">The player's name of the current turn.</param>
        [OperationContract(IsOneWay = true)]
        void ReportActivity(string matchCode, string currentTurnPlayerName);
    }
}
