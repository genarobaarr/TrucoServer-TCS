using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Mapping;
using TrucoServer.Helpers.Password;
using TrucoServer.Helpers.Profiles;
using TrucoServer.Helpers.Ranking;
using TrucoServer.Helpers.Security;
using TrucoServer.Helpers.Sessions;
using TrucoServer.Helpers.Verification;
using TrucoServer.Langs;
using TrucoServer.Security;
using TrucoServer.Utilities;

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
        private readonly baseDatosTrucoEntities context;
        private readonly BanService banService;

        private static readonly IUserSessionManager sessionManagerStatic = new UserSessionManager();

        public TrucoUserServiceImp()
        {
            this.context = new baseDatosTrucoEntities();

            var authHelper = new UserAuthenticationHelper();
            var sessionMgr = new UserSessionManager();
            var emailSvc = new EmailSender();
            var profileUpd = new ProfileUpdater(this.context);
            var passMgr = new PasswordManager(emailSvc);
            var mapper = new UserMapper();
            var ranking = new RankingService();
            var history = new MatchHistoryService();

            var verifySvc = new VerificationService(authHelper, emailSvc);
            var banSvc = new BanService(this.context);

            var dependencies = new TrucoUserServiceDependencies
            {
                AuthenticationHelper = authHelper,
                SessionManager = sessionMgr,
                EmailSender = emailSvc,
                VerificationService = verifySvc,
                ProfileUpdater = profileUpd,
                PasswordManager = passMgr,
                UserMapper = mapper,
                RankingService = ranking,
                MatchHistoryService = history,
                BanService = banSvc
            };

            this.authenticationHelper = dependencies.AuthenticationHelper;
            this.sessionManager = dependencies.SessionManager;
            this.emailSender = dependencies.EmailSender;
            this.verificationService = dependencies.VerificationService;
            this.profileUpdater = dependencies.ProfileUpdater;
            this.passwordManager = dependencies.PasswordManager;
            this.userMapper = dependencies.UserMapper;
            this.rankingService = dependencies.RankingService;
            this.matchHistoryService = dependencies.MatchHistoryService;
            this.banService = dependencies.BanService;
        }

        public TrucoUserServiceImp(baseDatosTrucoEntities context, TrucoUserServiceDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            this.context = context ?? throw new ArgumentNullException(nameof(context));

            this.authenticationHelper = dependencies.AuthenticationHelper;
            this.sessionManager = dependencies.SessionManager;
            this.emailSender = dependencies.EmailSender;
            this.verificationService = dependencies.VerificationService;
            this.profileUpdater = dependencies.ProfileUpdater;
            this.passwordManager = dependencies.PasswordManager;
            this.userMapper = dependencies.UserMapper;
            this.rankingService = dependencies.RankingService;
            this.matchHistoryService = dependencies.MatchHistoryService;

            this.banService = dependencies.BanService ?? new BanService(context);
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

                banService.ValidateBanStatus(username);

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
            catch (FaultException)
            {
                throw;
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

                profileUpdater.CreateAndSaveDefaultProfile(newUser.userID);

                return true;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(Register));
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(Register));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(Register));
                return false;
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
                User user = context.User.Include(u => u.UserProfile).SingleOrDefault(u => u.email == profile.Email);

                if (user == null)
                {
                    return false;
                }

                var updateContext = new UsernameUpdateContext
                {
                    User = user,
                    NewUsername = profile.Username,
                    MaxNameChanges = MAX_NAME_CHANGES
                };

                if (!profileUpdater.TryUpdateUsername(updateContext))
                {
                    return false;
                }

                var updateOptions = new ProfileUpdateOptions
                {
                    ProfileData = profile,
                    DefaultLanguageCode = DEFAULT_LANG_CODE,
                    DefaultAvatarId = DEFAULT_AVATAR_ID
                };

                profileUpdater.UpdateProfileDetails(user, updateOptions);

                context.SaveChanges();
                return true;
            }
            catch (JsonSerializationException ex)
            {
                ServerException.HandleException(ex, $"{nameof(SaveUserProfile)} - JSON Serialization Error");
                return false;
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(SaveUserProfile));
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(SaveUserProfile));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(SaveUserProfile));
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
                bool result = profileUpdater.ProcessAvatarUpdate(username, newAvatarId);
                return Task.FromResult(result);
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(UpdateUserAvatarAsync));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(UpdateUserAvatarAsync));
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

            var passwordUpdate = new PasswordUpdateOptions
            {
                Email = email,
                NewPassword = newPassword,
                LanguageCode = languageCode,
                CallingMethod = nameof(PasswordChange)
            };

            return passwordManager.UpdatePasswordAndNotify(passwordUpdate);
        }

        public bool PasswordReset(PasswordResetOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!ServerValidator.IsEmailValid(options.Email) || !ServerValidator.IsPasswordValid(options.NewPassword))
            {
                return false;
            }

            if (!verificationService.ConfirmEmailVerification(options.Email, options.Code))
            {
                return false;
            }

            var passwordUpdate = new PasswordUpdateOptions
            {
                Email = options.Email,
                NewPassword = options.NewPassword,
                LanguageCode = options.LanguageCode,
                CallingMethod = nameof(PasswordReset)
            };

            return passwordManager.UpdatePasswordAndNotify(passwordUpdate);
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            if (!ServerValidator.IsEmailValid(email))
            {
                return false;
            }

            if (verificationService == null)
            {
                return false;
            }

            try
            {
                return verificationService.RequestEmailVerification(email, languageCode);
            }
            catch (SmtpFailedRecipientsException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
            }
            catch (SmtpException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
            }
            catch (WebException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
            }
            catch (CultureNotFoundException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
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
                return context.User.Any(u => u.username == username);
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(UsernameExists));
                throw FaultFactory.CreateFault("ServerDBErrorUser", Lang.ExceptionTextDBErrorUsernameExists);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(UsernameExists));
                throw FaultFactory.CreateFault("ServerDBErrorUser", Lang.ExceptionTextDBErrorUsernameExists);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(UsernameExists));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(UsernameExists));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
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
                return context.User.Any(u => u.email == email);
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(EmailExists));
                throw FaultFactory.CreateFault("ServerDBErrorEmail", Lang.ExceptionTextDBErrorUsernameExists);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(UsernameExists));
                throw FaultFactory.CreateFault("ServerDBErrorEmail", Lang.ExceptionTextDBErrorUsernameExists);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(EmailExists));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(EmailExists));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
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
                User user = context.User.Include(u => u.UserProfile).FirstOrDefault(u => u.username == username);

                if (user == null)
                {
                    return null;
                }

                return userMapper.MapUserToProfileData(user);
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetUserProfile));
                return null;
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(GetUserProfile));
                return null;
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
                var user = await context.User.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.email == email);

                if (user == null)
                {
                    return null;
                }

                return userMapper.MapUserToProfileData(user);
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetUserProfileByEmailAsync));
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetUserProfileByEmailAsync));
                return null;
            }
        }

        public List<PlayerStats> GetGlobalRanking()
        {
            try
            {
                return rankingService.GetGlobalRanking();
            }

            catch (FaultException<CustomFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetGlobalRanking));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
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

            catch (FaultException<CustomFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetLastMatches));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
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
            catch (KeyNotFoundException ex)
            {
                ServerException.HandleException(ex, nameof(GetUserCallback));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetUserCallback));
            }

            return null;
        }

        public void LogClientException(string errorMessage, string stackTrace, string clientUsername)
        {
            try
            {
                string formattedLog = $"[CLIENT-ERROR] User: {clientUsername ?? "Anonymous"} | Msg: {errorMessage}";

                LogManager.LogWarn($"{formattedLog} | Stack: {stackTrace}", "ClientReport");
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(LogClientException));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(LogClientException));
            }
        }
    }
}
