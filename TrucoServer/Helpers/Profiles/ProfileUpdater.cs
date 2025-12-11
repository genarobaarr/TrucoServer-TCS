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
        private const string DEFAULT_LANGUAGE = "es-MX";
        private const string DEAULT_AVATAR_ID = "avatar_aaa_default";

        private readonly baseDatosTrucoEntities context;

        public ProfileUpdater(baseDatosTrucoEntities context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }
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

        public void CreateAndSaveDefaultProfile(int userId)
        {
            UserProfile profile = new UserProfile
            {
                userID = userId,
                avatarID = DEAULT_AVATAR_ID,
                socialLinksJson = Encoding.UTF8.GetBytes("{}"),
                languageCode = DEFAULT_LANGUAGE,
                isMusicMuted = false,
            };

            context.UserProfile.Add(profile);
            context.SaveChanges();
        }

        public bool TryUpdateUsername(UsernameUpdateContext updateContext)
        {
            var user = updateContext.User;
            var newUsername = updateContext.NewUsername;
            var maxChanges = updateContext.MaxNameChanges;

            if (string.Equals(user.username, newUsername, StringComparison.Ordinal))
            {
                return true;
            }

            if (user.nameChangeCount >= maxChanges)
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

        public void UpdateProfileDetails(User user, ProfileUpdateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.ProfileData == null)
            {
                throw new InvalidOperationException("Profile data cannot be null");
            }

            EnsureUserProfileExists(user, options);

            var userProfile = user.UserProfile;
            var inputData = options.ProfileData;

            UpdateAvatar(userProfile, inputData, options.DefaultAvatarId);
            UpdateLanguageSettings(userProfile, inputData, options.DefaultLanguageCode);
            UpdateSocialLinks(userProfile, inputData);
        }

        private static void UpdateAvatar(UserProfile userProfile, UserProfileData data, string defaultAvatar)
        {
            userProfile.avatarID = data.AvatarId ?? userProfile.avatarID ?? defaultAvatar;
        }

        private static void UpdateLanguageSettings(UserProfile userProfile, UserProfileData data, string defaultLang)
        {
            if (!string.IsNullOrWhiteSpace(data.LanguageCode))
            {
                userProfile.languageCode = data.LanguageCode;
                userProfile.isMusicMuted = data.IsMusicMuted;
            }
            else if (string.IsNullOrWhiteSpace(userProfile.languageCode))
            {
                userProfile.languageCode = defaultLang;
            }
        }

        private void UpdateSocialLinks(UserProfile userProfile, UserProfileData data)
        {
            var links = new SocialLinks
            {
                FacebookHandle = (data.FacebookHandle ?? string.Empty).Trim(),
                XHandle = (data.XHandle ?? string.Empty).Trim(),
                InstagramHandle = (data.InstagramHandle ?? string.Empty).Trim()
            };

            string json = JsonConvert.SerializeObject(links);
            userProfile.socialLinksJson = Encoding.UTF8.GetBytes(json);
        }

        public void EnsureUserProfileExists(User user, ProfileUpdateOptions options)
        {
            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    userID = user.userID,
                    languageCode = options.DefaultLanguageCode,
                    isMusicMuted = false,
                    avatarID = options.DefaultAvatarId,
                    socialLinksJson = Encoding.UTF8.GetBytes("{}")
                };

                context.UserProfile.Add(user.UserProfile);
            }
        }

        public bool ProcessAvatarUpdate(string username, string newAvatarId)
        {
            User user = context.User.FirstOrDefault(u => u.username == username);
            
            if (user == null)
            {
                return false;
            }

            UserProfile profile = context.UserProfile.FirstOrDefault(p => p.userID == user.userID);

            if (profile == null)
            {
                var defaultOptions = new ProfileUpdateOptions
                {
                    DefaultLanguageCode = DEFAULT_LANGUAGE,
                    DefaultAvatarId = DEAULT_AVATAR_ID
                };
                EnsureUserProfileExists(user, defaultOptions);
                profile = user.UserProfile;
            }

            profile.avatarID = newAvatarId;
            context.SaveChanges();
           
            return true;
        }
    }
}
