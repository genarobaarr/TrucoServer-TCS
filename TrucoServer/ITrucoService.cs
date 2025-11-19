using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace TrucoServer
{
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoUserService
    {
        [OperationContract]
        bool RequestEmailVerification(string email, string languageCode);

        [OperationContract]
        bool ConfirmEmailVerification(string email, string code);

        [OperationContract]
        bool Register(string username, string password, string email);

        [OperationContract]
        bool UsernameExists(string username);

        [OperationContract]
        bool EmailExists(string email);

        [OperationContract]
        bool Login(string username, string password, string languageCode);

        [OperationContract]
        Task<bool> UpdateUserAvatarAsync(string username, string newAvatarId);

        [OperationContract]
        Task<UserProfileData> GetUserProfileByEmailAsync(string email);

        [OperationContract]
        List<string> GetOnlinePlayers();

        [OperationContract]
        List<PlayerStats> GetGlobalRanking();

        [OperationContract]
        List<MatchScore> GetLastMatches(string username);

        [OperationContract]
        UserProfileData GetUserProfile(string username);

        [OperationContract]
        bool SaveUserProfile(UserProfileData profile);

        [OperationContract]
        bool PasswordReset(string email, string code, string newPassword, string languageCode);

        [OperationContract]
        bool PasswordChange(string email, string newPassword, string languageCode);
    }

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