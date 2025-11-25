using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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

namespace TrucoServer.Services
{
    public class TrucoUserServiceImp : ITrucoUserService
    {
        private const int MAX_NAME_CHANGES = 2;
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";

        private static readonly ConcurrentDictionary<string, string> verificationCodes = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, ITrucoCallback> onlineUsers = new ConcurrentDictionary<string, ITrucoCallback>();

        public bool Login(string username, string password, string languageCode)
        {
            try
            {
                LanguageManager.SetLanguage(languageCode);

                ValidateBruteForceStatus(username);

                User user = AuthenticateUser(username, password);

                if (user == null)
                {
                    return false;
                }

                string realUsername = user.username;

                HandleExistingSession(realUsername);

                return TryRegisterAndNotify(user, username);
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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (context.User.Any(u => u.email == email || u.username == username))
                    {
                        return false;
                    }

                    User user = new User
                    {
                        username = username,
                        passwordHash = PasswordHasher.Hash(password),
                        email = email,
                        wins = 0,
                        nameChangeCount = 0
                    };

                    context.User.Add(user);
                    context.SaveChanges();

                    UserProfile profile = new UserProfile
                    {
                        userID = user.userID,
                        avatarID = DEFAULT_AVATAR_ID,
                        socialLinksJson = Encoding.UTF8.GetBytes("{}")
                    };
                    context.UserProfile.Add(profile);
                    context.SaveChanges();

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
                LogManager.LogError(ex, nameof(Register));
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(Register)} - SQL Server Error");
                return false;
            }
            catch (ArgumentException ex)
            {
                LogManager.LogError(ex, nameof(Register));
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(Register));
                return false;
            }
        }

        public bool UsernameExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
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
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(UsernameExists)} - Invalid Operation (DataBase Context)");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(UsernameExists));
                throw;
            }
        }

        public bool EmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
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
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(EmailExists)} - Invalid Operation (DataBase Context)");
                throw;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(EmailExists));
                throw;
            }
        }

        public UserProfileData GetUserProfile(string username)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.Include(u => u.UserProfile).FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        return null;
                    }

                    SocialLinks links = new SocialLinks();

                    if (user.UserProfile?.socialLinksJson != null)
                    {
                        string json = Encoding.UTF8.GetString(user.UserProfile.socialLinksJson);
                        links = JsonConvert.DeserializeObject<SocialLinks>(json) ?? new SocialLinks();
                    }

                    return new UserProfileData
                    {
                        Username = user.username,
                        Email = user.email,
                        AvatarId = user.UserProfile?.avatarID ?? DEFAULT_AVATAR_ID,
                        NameChangeCount = user.nameChangeCount,
                        FacebookHandle = links.FacebookHandle ?? "",
                        XHandle = links.XHandle ?? "",
                        InstagramHandle = links.InstagramHandle ?? ""
                    };
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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = await context.User.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.email == email);

                    if (user == null)
                    {
                        return null;
                    }

                    SocialLinks links = new SocialLinks();

                    if (user.UserProfile?.socialLinksJson != null)
                    {
                        string json = Encoding.UTF8.GetString(user.UserProfile.socialLinksJson);
                        links = JsonConvert.DeserializeObject<SocialLinks>(json) ?? new SocialLinks();
                    }

                    return new UserProfileData
                    {
                        Username = user.username,
                        Email = user.email,
                        AvatarId = user.UserProfile?.avatarID ?? DEFAULT_AVATAR_ID,
                        NameChangeCount = user.nameChangeCount,
                        FacebookHandle = links?.FacebookHandle ?? "",
                        XHandle = links?.XHandle ?? "",
                        InstagramHandle = links?.InstagramHandle ?? ""
                    };
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

        public bool SaveUserProfile(UserProfileData profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Email))
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

                    if (user.username != profile.Username)
                    {
                        if (user.nameChangeCount >= MAX_NAME_CHANGES)
                        {
                            return false;
                        }

                        if (context.User.Any(u => u.username == profile.Username && u.userID != user.userID))
                        {
                            return false;
                        }


                        user.username = profile.Username;
                        user.nameChangeCount++;
                    }

                    if (user.UserProfile == null)
                    {
                        user.UserProfile = new UserProfile { userID = user.userID };
                        context.UserProfile.Add(user.UserProfile);
                    }

                    user.UserProfile.avatarID = profile.AvatarId ?? DEFAULT_AVATAR_ID;

                    var links = new SocialLinks
                    {
                        FacebookHandle = profile.FacebookHandle?.Trim() ?? "",
                        XHandle = profile.XHandle?.Trim() ?? "",
                        InstagramHandle = profile.InstagramHandle?.Trim() ?? ""
                    };

                    string json = JsonConvert.SerializeObject(links);
                    user.UserProfile.socialLinksJson = Encoding.UTF8.GetBytes(json);

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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        return Task.FromResult(false);
                    }

                    UserProfile profile = context.UserProfile.FirstOrDefault(p => p.userID == user.userID);

                    if (profile == null)
                    {
                        profile = new UserProfile { userID = user.userID, socialLinksJson = Encoding.UTF8.GetBytes("{}") };
                        context.UserProfile.Add(profile);
                    }

                    profile.avatarID = newAvatarId;
                    context.SaveChanges();
                    return Task.FromResult(true);
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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.FirstOrDefault(u => u.email == email);

                    if (user == null)
                    {
                        return false;
                    }

                    user.passwordHash = PasswordHasher.Hash(newPassword);
                    context.SaveChanges();

                    LanguageManager.SetLanguage(languageCode);

                    Task.Run(() => SendEmail(user.email, Lang.EmailPasswordNotificationSubject,
                        string.Format(Lang.EmailPasswordNotificationBody, user.username, DateTime.Now).Replace("\\n", Environment.NewLine)));

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(PasswordChange)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(PasswordChange)} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, $"{nameof(PasswordChange)} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(PasswordChange));
                return false;
            }
        }

        public bool PasswordReset(string email, string code, string newPassword, string languageCode)
        {
            if (!ConfirmEmailVerification(email, code))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.FirstOrDefault(u => u.email == email);

                    if (user == null)
                    {
                        return false;
                    }

                    user.passwordHash = PasswordHasher.Hash(newPassword);
                    context.SaveChanges();

                    LanguageManager.SetLanguage(languageCode);

                    Task.Run(() => SendEmail(user.email, Lang.EmailPasswordNotificationSubject,
                        string.Format(Lang.EmailPasswordNotificationBody, user.username, DateTime.Now).Replace("\\n", Environment.NewLine)));

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{nameof(PasswordReset)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(PasswordReset)} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, $"{nameof(PasswordReset)} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(PasswordReset));
                return false;
            }
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            try
            {
                string code = GenerateSecureNumericCode();

                verificationCodes[email] = code;

                LanguageManager.SetLanguage(languageCode);

                Task.Run(() => SendEmail(email, Lang.EmailVerificationSubject,
                    string.Format(Lang.EmailVerificationBody, code).Replace("\\n", Environment.NewLine)));

                Console.WriteLine($"[EMAIL] Code Sended To {email}: {code}");

                return true;
            }
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, nameof(RequestEmailVerification));
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, nameof(RequestEmailVerification));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(RequestEmailVerification));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RequestEmailVerification));
            }
            return false;
        }

        public bool ConfirmEmailVerification(string email, string code)
        {
            if (verificationCodes.TryGetValue(email, out string storedCode) && storedCode == code)
            {
                verificationCodes.TryRemove(email, out _);
                return true;
            }

            return false;
        }

        public List<PlayerStats> GetGlobalRanking()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var topPlayers = context.User
                        .OrderByDescending(u => u.wins)
                        .Take(10)
                        .Select(u => new PlayerStats
                        {
                            PlayerName = u.username,
                            Wins = u.wins,
                        })
                        .ToList();

                    return topPlayers;
                }
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
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        LogManager.LogWarn($"History attempt for user not found: {username}", nameof(GetLastMatches));
                        return new List<MatchScore>();
                    }

                    int userID = user.userID;

                    var lastMatches = context.MatchPlayer
                        .Where(mp => mp.userID == userID)
                        .Select(mp => new { MatchPlayer = mp, Match = mp.Match })
                        .Where(join => join.Match.status == "Finished" && join.Match.endedAt.HasValue)
                        .OrderByDescending(join => join.Match.endedAt)
                        .Take(5)
                        .Select(join => new MatchScore
                        {
                            MatchID = join.Match.matchID.ToString(),
                            EndedAt = join.Match.endedAt.Value,
                            IsWin = join.MatchPlayer.isWinner,
                            FinalScore = join.MatchPlayer.score
                        })
                        .ToList();

                    return lastMatches;
                }
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

        private void ValidateBruteForceStatus(string username)
        {
            if (BruteForceProtector.IsBlocked(username))
            {
                var fault = new LoginFault
                {
                    ErrorCode = "TooManyAttempts",
                    ErrorMessage = Lang.ExceptionTextTooManyAttempts
                };

                throw new FaultException<LoginFault>(fault, new FaultReason("TooManyAttempts"));
            }
        }

        private User AuthenticateUser(string username, string password)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.email == username || u.username == username);

                    if (user == null || !PasswordHasher.Verify(password, user.passwordHash))
                    {
                        BruteForceProtector.RegisterFailedAttempt(username);
                        return null;
                    }

                    return user;
                }
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(Login)} - Database Error");
                return null;
            }
        }

        private void HandleExistingSession(string realUsername)
        {
            if (!onlineUsers.ContainsKey(realUsername))
            {
                return;
            }

            var oldCallback = onlineUsers[realUsername];
            var oldChannel = oldCallback as ICommunicationObject;
            bool isZombie = true;

            if (oldChannel != null && oldChannel.State == CommunicationState.Opened)
            {
                oldCallback.Ping();
                isZombie = false;
            }

            if (!isZombie)
            {
                var fault = new LoginFault
                {
                    ErrorCode = "UserAlreadyLoggedIn",
                    ErrorMessage = Lang.ExceptionTextLogin
                };

                throw new FaultException<LoginFault>(fault, new FaultReason("UserAlreadyLoggedIn"));
            }
            else
            {
                ITrucoCallback trash;
                onlineUsers.TryRemove(realUsername, out trash);
            }
        }

        private bool TryRegisterAndNotify(User user, string inputUsername)
        {
            try
            {
                RegisterSession(user.username);
                SendLoginEmailAsync(user);
                BruteForceProtector.RegisterSuccess(inputUsername);

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

        private void RegisterSession(string realUsername)
        {
            ITrucoCallback currentCallback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
            onlineUsers.AddOrUpdate(realUsername, currentCallback, (key, oldValue) => currentCallback);
        }

        private void SendLoginEmailAsync(User user)
        {
            Task.Run(() =>
            {
                try
                {
                    SendEmail(user.email, Lang.EmailLoginNotificationSubject,
                        string.Format(Lang.EmailLoginNotificactionBody, user.username, DateTime.Now)
                        .Replace("\\n", Environment.NewLine));
                }
                catch (SmtpException ex)
                {
                    LogManager.LogError(ex, "Login_EmailTask");
                }
            });
        }

        private void SendEmail(string toEmail, string emailSubject, string emailBody)
        {
            try
            {
                var settings = ConfigurationReader.EmailSettings;
                var fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
                var toAddress = new MailAddress(toEmail);

                using (var smtp = new SmtpClient
                {
                    Host = settings.SmtpHost,
                    Port = settings.SmtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, settings.FromPassword)
                })
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = emailSubject,
                    Body = emailBody
                })
                {
                    smtp.Send(message);
                }
            }
            catch (SmtpFailedRecipientException ex)
            {
                LogManager.LogError(ex, nameof(SendEmail));
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, nameof(SendEmail));
            }
            catch (FormatException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendEmail)} - Invalid Email Format");
            }
            catch (ConfigurationErrorsException ex)
            {
                LogManager.LogError(ex, $"{nameof(SendEmail)} - Configuration Error");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SendEmail));
            }
        }

        private static string GenerateSecureNumericCode()
        {
            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] buffer = new byte[4];

                    rng.GetBytes(buffer);
                    int secureInt = BitConverter.ToInt32(buffer, 0);

                    secureInt = Math.Abs(secureInt);

                    string secureCode = (secureInt % 900000 + 100000).ToString();

                    return secureCode;
                }
            }
            catch (CryptographicException ex)
            {
                LogManager.LogError(ex, $"{nameof(GenerateSecureNumericCode)} - Cryptographic Provider Error");
                return "000000";
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GenerateSecureNumericCode));
                return "000000";
            }
        }

        public static ITrucoCallback GetUserCallback(string username)
        {
            try
            {
                if (onlineUsers.TryGetValue(username, out ITrucoCallback callback))
                {
                    var communicationObject = (ICommunicationObject)callback;
                    if (communicationObject.State == CommunicationState.Opened)
                    {
                        return callback;
                    }

                    try
                    {
                        communicationObject.Abort();
                    }
                    catch
                    {
                        /* noop */
                    }
                    onlineUsers.TryRemove(username, out _);
                }
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"[ERROR] Communication interrupted for {username}: {ex.Message}.");
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine($"[ERROR] Callback object conversion failed for {username}: {ex.Message}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error getting callback from {username}: {ex.Message}.");
            }

            return null;
        }
    }
}