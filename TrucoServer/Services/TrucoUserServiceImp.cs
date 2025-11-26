using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
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
            this.match_history_instantiation_guard();
            this.matchHistoryService = new MatchHistoryService();
        }
        private void match_history_instantiation_guard()
        {
            // intentionally empty - preserves style/formatting
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

                Console.WriteLine($"[LOGIN] User {user.username} logged in successfully.");
                return true;
            }
            catch (FaultException)
            {
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Source.Contains("System.ServiceModel"))
            {
                LogManager.LogError(ex, $"{nameof(Login)} - WCF Context Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(Login)} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, $"{nameof(Login)} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(Login));
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

                    Console.WriteLine($"[REGISTER] New user registered: {username}");
                    return true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, $"{nameof(Register)} - Entity Validation Error");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(Register)} - Database Update Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(Register)} - SQL Server Error");
                return false;
            }
            catch (ArgumentException ex)
            {
                LogManager.LogError(ex, $"{nameof(Register)} - Argument Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(Register));
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
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(SaveUserProfile)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(SaveUserProfile)} - SQL Server Error");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveUserProfile));
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
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(UpdateUserAvatarAsync)} - DataBase Saving Error");
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(UpdateUserAvatarAsync)} - SQL Server Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(UpdateUserAvatarAsync));
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
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, $"{nameof(RequestEmailVerification)} - Argument Null");
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, $"{nameof(RequestEmailVerification)} - SMTP Error");
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(RequestEmailVerification)} - Invalid Operation");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RequestEmailVerification));
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
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(UsernameExists)} - SQL Server Error");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(UsernameExists)} - Invalid Operation (DataBase Context)");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(UsernameExists));
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
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(EmailExists)} - SQL Server Error");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(EmailExists)} - Invalid Operation (DataBase Context)");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(EmailExists));
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
            catch (JsonException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserProfile)} - JSON Deserialization Error");
                return null;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserProfile)} - SQL Server Error");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserProfile)} - Invalid Operation (DataBase Context)");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetUserProfile));
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
            catch (JsonException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserProfileByEmailAsync)} - JSON Deserialization Error");
                return null;
            }
            catch (System.Data.Common.DbException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserProfileByEmailAsync)} - Database Query Error");
                return null;
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
            catch (NotSupportedException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetGlobalRanking)} - LINQ Not Supported");
                return new List<PlayerStats>();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetGlobalRanking)} - SQL Error");
                return new List<PlayerStats>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetGlobalRanking)} - Invalid Operation (DataBase Context)");
                return new List<PlayerStats>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetGlobalRanking));
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
            catch (NotSupportedException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLastMatches)} - LINQ Not Supported");
                return new List<MatchScore>();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLastMatches)} - SQL Error");
                return new List<MatchScore>();
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetLastMatches)} - Invalid Operation (DataBase Context)");
                return new List<MatchScore>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetLastMatches));
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
            catch (CommunicationException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserCallback)} - Communication interrupted for {username}");
            }
            catch (InvalidCastException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserCallback)} - Callback object conversion failed for {username}");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, $"{nameof(GetUserCallback)} - Error getting callback from {username}");
            }

            return null;
        }
    }
}
