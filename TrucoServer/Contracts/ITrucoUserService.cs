using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoUserService
    {
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool RequestEmailVerification(string email, string languageCode);

        [OperationContract]
        bool ConfirmEmailVerification(string email, string code);

        [OperationContract]
        bool Register(string username, string password, string email);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool UsernameExists(string username);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool EmailExists(string email);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
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
        bool PasswordReset(PasswordResetOptions options);

        [OperationContract]
        bool PasswordChange(string email, string newPassword, string languageCode);

        [OperationContract(IsOneWay = true)]
        void LogClientException(string errorMessage, string stackTrace, string clientUsername);
    }
}
