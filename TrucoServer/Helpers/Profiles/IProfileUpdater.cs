using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Profiles
{
    public interface IProfileUpdater
    {
        bool ValidateProfileInput(UserProfileData profile);
        void CreateAndSaveDefaultProfile(baseDatosTrucoEntities context, int userId);
        bool TryUpdateUsername(baseDatosTrucoEntities context, User user, string newUsername, int maxNameChanges);
        void UpdateProfileDetails(baseDatosTrucoEntities context, User user, ProfileUpdateOptions options);
        void EnsureUserProfileExists(baseDatosTrucoEntities context, User user, ProfileUpdateOptions options);
        bool ProcessAvatarUpdate(baseDatosTrucoEntities context, string username, string newAvatarId);
    }
}
