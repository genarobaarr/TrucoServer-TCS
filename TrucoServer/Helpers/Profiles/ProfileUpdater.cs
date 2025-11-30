using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Profiles
{
    public class ProfileUpdater : IProfileUpdater
    {
        public bool ValidateProfileInput(UserProfileData profile)
        {
            if (profile == null)
            {
                return false;
            }

            bool isEmailValid = ServerValidator.IsEmailValid(profile.Email);
            bool isUsernameValid = ServerValidator.IsUsernameValid(profile.Username);

            return isEmailValid && isUsernameValid;
        }

        public void CreateAndSaveDefaultProfile(baseDatosTrucoEntities context, int userId)
        {
            UserProfile profile = new UserProfile
            {
                userID = userId,
                avatarID = "avatar_aaa_default",
                socialLinksJson = Encoding.UTF8.GetBytes("{}")
            };

            context.UserProfile.Add(profile);
            context.SaveChanges();
        }

        public bool TryUpdateUsername(baseDatosTrucoEntities context, User user, string newUsername, int maxNameChanges)
        {
            if (string.Equals(user.username, newUsername, StringComparison.Ordinal))
            {
                return true;
            }

            if (user.nameChangeCount >= maxNameChanges)
            {
                return false;
            }

            if (context.User.Any(u => u.username == newUsername && u.userID != user.userID))
            {
                return false;
            }

            user.username = newUsername;
            user.nameChangeCount++;
            
            return true;
        }

        public void UpdateProfileDetails(baseDatosTrucoEntities context, User user, UserProfileData profile, string defaultLangCode, string defaultAvatar)
        {
            EnsureUserProfileExists(context, user, defaultLangCode, defaultAvatar);

            user.UserProfile.avatarID = profile.AvatarId ?? user.UserProfile.avatarID ?? defaultAvatar;

            if (!string.IsNullOrWhiteSpace(profile.LanguageCode))
            {
                user.UserProfile.languageCode = profile.LanguageCode;
                user.UserProfile.isMusicMuted = profile.IsMusicMuted;
            }
            else if (string.IsNullOrWhiteSpace(user.UserProfile.languageCode))
            {
                user.UserProfile.languageCode = defaultLangCode;
            }

            var links = new SocialLinks
            {
                FacebookHandle = (profile.FacebookHandle ?? "").Trim(),
                XHandle = (profile.XHandle ?? "").Trim(),
                InstagramHandle = (profile.InstagramHandle ?? "").Trim()
            };

            string json = JsonConvert.SerializeObject(links);
            user.UserProfile.socialLinksJson = Encoding.UTF8.GetBytes(json);
        }

        public void EnsureUserProfileExists(baseDatosTrucoEntities context, User user, string defaultLangCode, string defaultAvatar)
        {
            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    userID = user.userID,
                    languageCode = defaultLangCode,
                    isMusicMuted = false,
                    avatarID = defaultAvatar,
                    socialLinksJson = Encoding.UTF8.GetBytes("{}")
                };

                context.UserProfile.Add(user.UserProfile);
            }
        }

        public bool ProcessAvatarUpdate(baseDatosTrucoEntities context, string username, string newAvatarId)
        {
            User user = context.User.FirstOrDefault(u => u.username == username);

            if (user == null)
            {
                return false;
            }

            UserProfile profile = context.UserProfile.FirstOrDefault(p => p.userID == user.userID);

            if (profile == null)
            {
                EnsureUserProfileExists(context, user, "es-MX", "avatar_aaa_default");
                profile = user.UserProfile;
            }

            profile.avatarID = newAvatarId;
            context.SaveChanges();
           
            return true;
        }
    }
}
