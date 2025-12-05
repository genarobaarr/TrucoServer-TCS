using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Profiles
{
    public interface IProfileUpdater
    {
        bool ValidateProfileInput(UserProfileData profile);
        void CreateAndSaveDefaultProfile(int userId);
        bool TryUpdateUsername(UsernameUpdateContext updateContext);
        void UpdateProfileDetails(User user, ProfileUpdateOptions options);
        void EnsureUserProfileExists(User user, ProfileUpdateOptions options);
        bool ProcessAvatarUpdate(string username, string newAvatarId);
    }
}
