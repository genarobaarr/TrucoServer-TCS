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
        bool RequestEmailVerification(string email);

        [OperationContract]
        bool ConfirmEmailVerification(string email, string code);

        [OperationContract]
        bool Register(string username, string password, string email);

        [OperationContract]
        bool Login(string username, string password);

        [OperationContract(IsOneWay = true)]
        void Logout(string username);

        [OperationContract]
        Task<bool> UpdateUserAvatarAsync(string username, string newAvatarId);

        [OperationContract]
        List<string> GetOnlinePlayers();

        [OperationContract]
        List<PlayerStats> GetGlobalRanking();

        [OperationContract]
        List<MatchResult> GetLastMatches(string username);

        [OperationContract]
        UserProfileData GetUserProfile(string username);

        [OperationContract]
        bool SaveUserProfile(UserProfileData profile);

        [OperationContract]
        bool SendPasswordResetCode(string username);

        [OperationContract]
        bool ResetPassword(string username, string code, string newPassword);
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