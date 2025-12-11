using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    /// <summary>
    /// Service contract for user management, authentication, and profiles.
    /// </summary>
    [ServiceContract(CallbackContract = typeof(ITrucoCallback))]
    public interface ITrucoUserService
    {
        /// <summary>
        /// Requests a verification code to be sent to the specified email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="languageCode">The language code for the email content.</param>
        /// <returns>True if the email was sent successfully; otherwise, False.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool RequestEmailVerification(string email, string languageCode);

        /// <summary>
        /// Confirms if the provided code matches the one sent to the email address.
        /// </summary>
        /// <param name="email">The email address to verify.</param>
        /// <param name="code">The numeric code entered by the user.</param>
        /// <returns>True if the code is correct; otherwise, False.</returns>
        [OperationContract]
        bool ConfirmEmailVerification(string email, string code);

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="username">The desired username.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="email">The user's email address.</param>
        /// <returns>True if registration was successful; otherwise, False.</returns>
        [OperationContract]
        bool Register(string username, string password, string email);

        /// <summary>
        /// Checks if a username already exists in the database.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if the username exists; otherwise, False.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool UsernameExists(string username);

        /// <summary>
        /// Checks if an email address is already registered in the system.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if the email exists; otherwise, False.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool EmailExists(string email);

        /// <summary>
        /// Logs a user in by validating their credentials.
        /// </summary>
        /// <param name="username">The username or email address.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="languageCode">The language code for the session context.</param>
        /// <returns>True if credentials are valid; otherwise, False.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool Login(string username, string password, string languageCode);

        /// <summary>
        /// Updates the user's avatar asynchronously.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="newAvatarId">The identifier of the new avatar.</param>
        /// <returns>A task representing the asynchronous operation, returning True if successful.</returns>
        [OperationContract]
        Task<bool> UpdateUserAvatarAsync(string username, string newAvatarId);

        /// <summary>
        /// Retrieves the user profile asynchronously by email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>A task containing the user profile data, or null if not found.</returns>
        [OperationContract]
        Task<UserProfileData> GetUserProfileByEmailAsync(string email);

        /// <summary>
        /// Retrieves the global ranking of top players.
        /// </summary>
        /// <returns>A list of player statistics ordered by wins.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<PlayerStats> GetGlobalRanking();

        /// <summary>
        /// Retrieves the history of the last matches played by a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>A list of match scores and results.</returns>
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<MatchScore> GetLastMatches(string username);

        /// <summary>
        /// Retrieves the full profile data for a specific user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>An object containing the user's profile information.</returns>
        [OperationContract]
        UserProfileData GetUserProfile(string username);

        /// <summary>
        /// Saves changes made to a user's profile.
        /// </summary>
        /// <param name="profile">The object containing updated profile data.</param>
        /// <returns>True if the profile was saved successfully; otherwise, False.</returns>
        [OperationContract]
        bool SaveUserProfile(UserProfileData profile);

        /// <summary>
        /// Resets a user's password using a verification code.
        /// </summary>
        /// <param name="options">Options including email, code, and the new password.</param>
        /// <returns>True if the reset was successful; otherwise, False.</returns>
        [OperationContract]
        bool PasswordReset(PasswordResetOptions options);

        /// <summary>
        /// Changes the password for an authenticated user.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <param name="languageCode">The language code for notification emails.</param>
        /// <returns>True if the password was changed successfully; otherwise, False.</returns>
        [OperationContract]
        bool PasswordChange(string email, string newPassword, string languageCode);

        /// <summary>
        /// Logs a client-side exception for monitoring purposes.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="stackTrace">The stack trace of the error.</param>
        /// <param name="clientUsername">The username reporting the error (optional).</param>
        [OperationContract(IsOneWay = true)]
        void LogClientException(string errorMessage, string stackTrace, string clientUsername);
    }
}
