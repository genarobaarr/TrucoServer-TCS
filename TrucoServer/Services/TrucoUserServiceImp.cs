using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Langs;
using TrucoServer.Security;
using TrucoServer.Utilities;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Sessions;
using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Verification;
using TrucoServer.Helpers.Profiles;
using TrucoServer.Helpers.Password;
using TrucoServer.Helpers.Mapping;
using TrucoServer.Helpers.Ranking;

namespace TrucoServer.Services
{
    public class TrucoUserServiceImp : ITrucoUserService
    {
        private const int MAX_NAME_CHANGES = 2;
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";
        private const string DEFAULT_LANG_CODE = "es-MX";

        private readonly IUserAuthenticationHelper authenticationHelper;
        private readonly IUserSessionManager sessionManager;
        private readonly IEmailSender emailSender;
        private readonly IVerificationService verificationService;
        private readonly IProfileUpdater profileUpdater;
        private readonly IPasswordManager passwordManager;
        private readonly IUserMapper userMapper;
        private readonly IRankingService rankingService;
        private readonly IMatchHistoryService matchHistoryService;

        private static readonly IUserSessionManager sessionManagerStatic = new UserSessionManager();

        public TrucoUserServiceImp()
        {
            this.authenticationHelper = new UserAuthenticationHelper();
            this.sessionManager = new UserSessionManager();
            this.emailSender = new EmailSender();
            this.verificationService = new VerificationService(authenticationHelper, emailSender);
            this.profileUpdater = new ProfileUpdater();
            this.passwordManager = new PasswordManager(emailSender);
            this.userMapper = new UserMapper();
            this.rankingService = new RankingService();
            this.matchHistoryService = new MatchHistoryService();
        }

        public bool Login(string username, string password, string languageCode)
        {
            bool isUsernameValid = ServerValidator.IsUsernameValid(username);
            bool isEmailValid = ServerValidator.IsEmailValid(username);

            if ((!isUsernameValid && !isEmailValid) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                LanguageManager.SetLanguage(languageCode);

                authenticationHelper.ValidateBruteForceStatus(username);

                User user = authenticationHelper.AuthenticateUser(username, password);

                if (user == null)
                {
                    return false;
                }

                sessionManager.HandleExistingSession(user.username);

                sessionManager.RegisterSession(user.username);
                emailSender.SendLoginEmailAsync(user);
                BruteForceProtector.RegisterSuccess(username);

                return true;
            }
            catch (InvalidOperationException ex) when (ex.Source.Contains("System.ServiceModel"))
            {
                ServerException.HandleException(ex, nameof(Login));
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(Login));
                return false;
            }
        }

        public bool Register(string username, string password, string email)
        {
            if (!ServerValidator.IsUsernameValid(username) ||
                !ServerValidator.IsEmailValid(email) ||
                !ServerValidator.IsPasswordValid(password))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (context.User.Any(u => u.email == email || u.username == username))
                    {
                        return false;
                    }

                    User newUser = new User
                    {
                        username = username,
                        passwordHash = PasswordHasher.Hash(password),
                        email = email,
                        wins = 0,
                        nameChangeCount = 0
                    };

                    context.User.Add(newUser);
                    context.SaveChanges();

                    profileUpdater.CreateAndSaveDefaultProfile(context, newUser.userID);

                    return true;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(Register));
                return false;
            }
        }

        public bool SaveUserProfile(UserProfileData profile)
        {
            if (!profileUpdater.ValidateProfileInput(profile))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.Include(u => u.UserProfile).SingleOrDefault(u => u.email == profile.Email);

                    if (user == null)
                    {
                        return false;
                    }

                    if (!profileUpdater.TryUpdateUsername(context, user, profile.Username, MAX_NAME_CHANGES))
                    {
                        return false;
                    }

                    profileUpdater.UpdateProfileDetails(context, user, profile, DEFAULT_LANG_CODE, DEFAULT_AVATAR_ID);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (JsonSerializationException ex)
            {
                LogManager.LogError(ex, $"{nameof(SaveUserProfile)} - JSON Serialization Error");
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SaveUserProfile));
                return false;
            }
        }

        public Task<bool> UpdateUserAvatarAsync(string username, string newAvatarId)
        {
            if (!ServerValidator.IsUsernameValid(username) || string.IsNullOrWhiteSpace(newAvatarId))
            {
                return Task.FromResult(false);
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    bool result = profileUpdater.ProcessAvatarUpdate(context, username, newAvatarId);
                    return Task.FromResult(result);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(UpdateUserAvatarAsync));
            }

            return Task.FromResult(false);
        }

        public bool PasswordChange(string email, string newPassword, string languageCode)
        {
            if (!ServerValidator.IsEmailValid(email) || !ServerValidator.IsPasswordValid(newPassword))
            {
                return false;
            }

            return passwordManager.UpdatePasswordAndNotify(email, newPassword, languageCode, nameof(PasswordChange));
        }

        public bool PasswordReset(string email, string code, string newPassword, string languageCode)
        {
            if (!ServerValidator.IsEmailValid(email) || !ServerValidator.IsPasswordValid(newPassword))
            {
                return false;
            }

            if (!verificationService.ConfirmEmailVerification(email, code))
            {
                return false;
            }

            return passwordManager.UpdatePasswordAndNotify(email, newPassword, languageCode, nameof(PasswordReset));
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            if (!ServerValidator.IsEmailValid(email))
            {
                return false;
            }

            try
            {
                return verificationService.RequestEmailVerification(email, languageCode);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
            }

            return false;
        }

        public bool ConfirmEmailVerification(string email, string code)
        {
            return verificationService.ConfirmEmailVerification(email, code);
        }

        public bool UsernameExists(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    return context.User.Any(u => u.username == username);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(UsernameExists));
                return false;
            }
        }

        public bool EmailExists(string email)
        {
            if (!ServerValidator.IsEmailValid(email))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    return context.User.Any(u => u.email == email);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(EmailExists));
                return false;
            }
        }

        public UserProfileData GetUserProfile(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return null;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.Include(u => u.UserProfile).FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        return null;
                    }

                    return userMapper.MapUserToProfileData(user);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetUserProfile));
                return null;
            }
        }

        public async Task<UserProfileData> GetUserProfileByEmailAsync(string email)
        {
            if (!ServerValidator.IsEmailValid(email))
            {
                return null;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = await context.User.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.email == email);

                    if (user == null)
                    {
                        return null;
                    }

                    return userMapper.MapUserToProfileData(user);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetUserProfileByEmailAsync));
                return null;
            }
        }

        public List<PlayerStats> GetGlobalRanking()
        {
            try
            {
                return rankingService.GetGlobalRanking();
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
        }

        public List<MatchScore> GetLastMatches(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return new List<MatchScore>();
            }

            try
            {
                return matchHistoryService.GetLastMatches(username);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLastMatches));
                return new List<MatchScore>();
            }
        }

        public List<string> GetOnlinePlayers()
        {
            throw new NotImplementedException();
        }

        public static ITrucoCallback GetUserCallback(string username)
        {
            try
            {
                return sessionManagerStatic.GetUserCallback(username);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetUserCallback));
            }

            return null;
        }
    }
}
