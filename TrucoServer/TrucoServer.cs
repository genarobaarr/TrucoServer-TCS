using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Data.Entity.Core;
using System.Threading;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Langs;

namespace TrucoServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class TrucoServer : ITrucoUserService, ITrucoFriendService, ITrucoMatchService
    {
        private const int MAX_NAME_CHANGES = 2;
        private const string DEFAULT_AVATAR_ID = "avatar_aaa_default";
        private const string ACCEPTED_STATUS = "Accepted";
        private const string EXPIRED_STATUS = "Expired";
        private const string PENDING_STATUS = "Pending";
        private const string PUBLIC_STATUS = "Public";
        private const string PRIVATE_STATUS = "Private";
        private const string CLOSED_STATUS = "Closed";
        private const string INPROGRESS_STATUS = "InProgress";
        private const string FINISHED_STATUS = "Finished";
        private static readonly Random randomNumberGenerator = new Random();
        private static readonly ConcurrentDictionary<string, string> verificationCodes = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, ITrucoCallback> onlineUsers = new ConcurrentDictionary<string, ITrucoCallback>();
        private readonly ConcurrentDictionary<string, int> matchCodeToLobbyId = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, List<ITrucoCallback>> matchCallbacks = new ConcurrentDictionary<string, List<ITrucoCallback>>();
        private static readonly ConcurrentDictionary<ITrucoCallback, string> matchCallbackToPlayerName = new ConcurrentDictionary<ITrucoCallback, string>();

        private static void LogError(Exception ex, string methodName)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR IN {methodName}] {ex.GetType().Name}: {ex.Message}");
            }
            catch (System.IO.IOException)
            {
                /* The error cannot be logged; it is simply ignored to avoid creating an error loop. */
            }
            catch (Exception)
            {
                /* noop */
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static ITrucoCallback GetUserCallback(string username)
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

        private void RemoveInactiveCallbacks(string matchCode)
        {
            if (string.IsNullOrEmpty(matchCode))
            {
                return;
            }

            try
            {
                lock (matchCallbacks)
                {
                    if (!matchCallbacks.TryGetValue(matchCode, out var list))
                    {
                        return;
                    }

                    list.RemoveAll(cb =>
                    {
                        try
                        {
                            var comm = (ICommunicationObject)cb;
                            if (comm.State != CommunicationState.Opened)
                            {
                                try
                                {
                                    comm.Abort();
                                }
                                catch
                                {
                                    /* noop */
                                }
                                return true;
                            }
                        }
                        catch (Exception)
                        {
                            return true;
                        }
                        return false;
                    });
                }
            }
            catch (SynchronizationLockException ex)
            {
                LogError(ex, $"{nameof(RemoveInactiveCallbacks)} - Synchronization Block Error");
            }
            catch (OutOfMemoryException ex)
            {
                LogError(ex, $"{nameof(RemoveInactiveCallbacks)} - Insufficient Memory For Operation");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(RemoveInactiveCallbacks));
            }
        }

        private static string GenerateMatchCode()
        {
            const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            try
            {
                lock (randomNumberGenerator)
                {
                    return new string(Enumerable.Repeat(CHARS, 6)
                        .Select(s => s[randomNumberGenerator.Next(s.Length)]).ToArray());
                }
            }
            catch (SynchronizationLockException ex)
            {
                LogError(ex, $"{nameof(GenerateMatchCode)} - Synchronization Block Error");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GenerateMatchCode));
                return string.Empty;
            }
        }

        private static int GenerateNumericCodeFromString(string code)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in code)
                {
                    hash = hash * 31 + c;
                }
                return Math.Abs(hash % 999999);
            }
        }

        private void BroadcastToMatchCallbacksAsync(string matchCode, Action<ITrucoCallback> invocation)
        {
            if (string.IsNullOrEmpty(matchCode) || invocation == null)
            {
                return;
            }

            try
            {
                if (!matchCallbacks.TryGetValue(matchCode, out var callbacksList))
                {
                    return;
                }

                var snapshot = callbacksList.ToArray();

                foreach (var cb in snapshot)
                {
                    Task.Run(() =>
                    {
                        ProcessSingleCallbackAsync(matchCode, cb, invocation);
                    });
                }
            }
            catch (OutOfMemoryException ex)
            {
                LogError(ex, $"{nameof(BroadcastToMatchCallbacksAsync)} - Insufficient Memory For Snapshot");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(BroadcastToMatchCallbacksAsync));
            }
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
            catch (SmtpException ex)
            {
                LogError(ex, nameof(SendEmail));
            }
            catch (FormatException ex)
            {
                LogError(ex, $"{nameof(SendEmail)} - Invalid Email Format");
            }
            catch (ConfigurationErrorsException ex)
            {
                LogError(ex, $"{nameof(SendEmail)} - Configuration Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SendEmail));
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
                LogError(ex, nameof(RequestEmailVerification));
            }
            catch (SmtpException ex)
            {
                LogError(ex, nameof(RequestEmailVerification));
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, nameof(RequestEmailVerification));
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(RequestEmailVerification));
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
                LogError(ex, $"{nameof(Register)} - Entity Validation Error");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, nameof(Register));
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(Register)} - SQL Server Error");
                return false;
            }
            catch (ArgumentException ex)
            {
                LogError(ex, nameof(Register));
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(Register));
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
                LogError(ex, $"{nameof(UsernameExists)} - SQL Server Error");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(UsernameExists)} - Invalid Operation (DataBase Context)");
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UsernameExists));
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
                LogError(ex, $"{nameof(UsernameExists)} - SQL Server Error");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(UsernameExists)} - Invalid Operation (DataBase Context)");
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UsernameExists));
                throw;
            }
        }

        public bool Login(string username, string password, string languageCode)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.FirstOrDefault(u => u.email == username || u.username == username);
                    if (user != null && PasswordHasher.Verify(password, user.passwordHash))
                    {
                        ITrucoCallback callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                        onlineUsers.TryAdd(user.username, callback);

                        LanguageManager.SetLanguage(languageCode);
                        Task.Run(() => SendEmail(user.email, Lang.EmailLoginNotificationSubject,
                            string.Format(Lang.EmailLoginNotificactionBody, username, DateTime.Now).Replace("\\n", Environment.NewLine)));

                        return true;
                    }
                    return false;
                }
            }
            catch (InvalidOperationException ex) when (ex.Source.Contains("System.ServiceModel"))
            {
                LogError(ex, $"{nameof(Login)} - WCF Context Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(Login)} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogError(ex, $"{nameof(Login)} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(Login));
                return false;
            }
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
                LogError(ex, $"{nameof(PasswordChange)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(PasswordChange)} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogError(ex, $"{nameof(PasswordChange)} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(PasswordChange));
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
                LogError(ex, $"{nameof(PasswordReset)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(PasswordReset)} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogError(ex, $"{nameof(PasswordReset)} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(PasswordReset));
                return false;
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

                    string json = user.UserProfile?.socialLinksJson != null
                        ? Encoding.UTF8.GetString(user.UserProfile.socialLinksJson)
                        : "{}";
                    dynamic links = JsonConvert.DeserializeObject(json);

                    return new UserProfileData
                    {
                        Username = user.username,
                        Email = user.email,
                        AvatarId = user.UserProfile?.avatarID ?? DEFAULT_AVATAR_ID,
                        NameChangeCount = user.nameChangeCount,
                        FacebookHandle = links?.facebook ?? "",
                        XHandle = links?.x ?? "",
                        InstagramHandle = links?.instagram ?? ""
                    };
                }
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                LogError(ex, $"{nameof(GetUserProfile)} - JSON Deserialization Error");
                return null;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(GetUserProfile)} - SQL Server Error");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(GetUserProfile)} - Invalid Operation (DataBase Context)");
                return null;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetUserProfile));
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

                    string json = user.UserProfile?.socialLinksJson != null ? Encoding.UTF8.GetString(user.UserProfile.socialLinksJson) : "{}";
                    dynamic links = JsonConvert.DeserializeObject(json);

                    return new UserProfileData
                    {
                        Username = user.username,
                        Email = user.email,
                        AvatarId = user.UserProfile?.avatarID ?? DEFAULT_AVATAR_ID,
                        NameChangeCount = user.nameChangeCount,
                        FacebookHandle = links?.facebook ?? "",
                        XHandle = links?.x ?? "",
                        InstagramHandle = links?.instagram ?? ""
                    };
                }
            }
            catch (JsonException ex)
            {
                LogError(ex, $"{nameof(GetUserProfileByEmailAsync)} - JSON Deserialization Error");
                return null;
            }
            catch (System.Data.Common.DbException ex)
            {
                LogError(ex, $"{nameof(GetUserProfileByEmailAsync)} - Database Query Error");
                return null;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetUserProfileByEmailAsync));
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
                    string json = JsonConvert.SerializeObject(new
                    {
                        facebook = profile.FacebookHandle ?? "",
                        x = profile.XHandle ?? "",
                        instagram = profile.InstagramHandle ?? ""
                    });
                    user.UserProfile.socialLinksJson = Encoding.UTF8.GetBytes(json);

                    context.SaveChanges();
                    return true;
                }
            }
            catch (JsonSerializationException ex)
            {
                LogError(ex, $"{nameof(SaveUserProfile)} - JSON Serialization Error");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(SaveUserProfile)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(SaveUserProfile)} - SQL Server Error");
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SaveUserProfile));
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
                LogError(ex, $"{nameof(UpdateUserAvatarAsync)} - DataBase Saving Error");
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(UpdateUserAvatarAsync)} - SQL Server Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UpdateUserAvatarAsync));
            }
            return Task.FromResult(false);
        }

        public bool SendFriendRequest(string fromUser, string toUser)
        {
            try
            {
                using (var db = new baseDatosTrucoEntities())
                {
                    User requester = db.User.FirstOrDefault(u => u.username == fromUser);
                    User target = db.User.FirstOrDefault(u => u.username == toUser);

                    if (requester == null || target == null)
                    {
                        return false;
                    }

                    int requesterId = requester.userID;
                    int targetId = target.userID;

                    bool friendshipExists = db.Friendship.Any(f =>
                        (f.userID == requesterId && f.friendID == targetId) ||
                        (f.userID == targetId && f.friendID == requesterId));

                    if (friendshipExists)
                    {
                        return false;
                    }
                    Friendship newRequest = new Friendship
                    {
                        userID = requesterId,
                        friendID = targetId,
                        status = PENDING_STATUS
                    };
                    db.Friendship.Add(newRequest);
                    db.SaveChanges();

                    var targetUserCallback = GetUserCallback(toUser);
                    targetUserCallback?.OnFriendRequestReceived(fromUser);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(SendFriendRequest)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(SendFriendRequest)} - SQL Server Error");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogError(ex, $"{nameof(SendFriendRequest)} - Callback Communication Error");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SendFriendRequest));
                return false;
            }
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            try
            {
                using (var db = new baseDatosTrucoEntities())
                {
                    User requester = db.User.FirstOrDefault(u => u.username == fromUser);
                    User acceptor = db.User.FirstOrDefault(u => u.username == toUser);
                    if (requester == null || acceptor == null)
                    {
                        return false;
                    }

                    Friendship request = db.Friendship.FirstOrDefault(f =>
                        f.userID == requester.userID &&
                        f.friendID == acceptor.userID &&
                        f.status == PENDING_STATUS);
                    if (request == null)
                    {
                        return false;
                    }

                    request.status = ACCEPTED_STATUS;

                    Friendship reciprocalFriendship = new Friendship
                    {
                        userID = acceptor.userID,
                        friendID = requester.userID,
                        status = ACCEPTED_STATUS
                    };
                    db.Friendship.Add(reciprocalFriendship);

                    db.SaveChanges();

                    var fromUserCallback = GetUserCallback(fromUser);
                    fromUserCallback?.OnFriendRequestAccepted(toUser);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(AcceptFriendRequest)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(AcceptFriendRequest)} - SQL Server Error");
                return false;
            }
            catch (CommunicationException ex)
            {
                LogError(ex, $"{nameof(AcceptFriendRequest)} - Callback Communication Error");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(AcceptFriendRequest));
                return false;
            }
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            try
            {
                using (var db = new baseDatosTrucoEntities())
                {
                    User u1 = db.User.FirstOrDefault(u => u.username == user1);
                    User u2 = db.User.FirstOrDefault(u => u.username == user2);
                    if (u1 == null || u2 == null)
                    {
                        return false;
                    }

                    List<Friendship> toRemove = db.Friendship.Where(f =>
                        (f.userID == u1.userID && f.friendID == u2.userID) ||
                        (f.userID == u2.userID && f.friendID == u1.userID)).ToList();
                    if (!toRemove.Any())
                    {
                        return false;
                    }

                    db.Friendship.RemoveRange(toRemove);
                    db.SaveChanges();

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(RemoveFriendOrRequest)} - DataBase Deletion Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(RemoveFriendOrRequest)} - SQL Server Error");
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(RemoveFriendOrRequest));
                return false;
            }
        }

        public List<FriendData> GetFriends(string username)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username.ToLower() == username.ToLower());
                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    int currentUserId = user.userID;

                    var friendsData = context.Friendship
                        .Where(f => (f.userID == currentUserId || f.friendID == currentUserId) && f.status == ACCEPTED_STATUS)
                        .Select(f => f.userID == currentUserId ? f.friendID : f.userID)
                        .Distinct()
                        .Join(context.User.Include("UserProfile"),
                              friendId => friendId,
                              u => u.userID,
                              (friendId, u) => new FriendData
                              {
                                  Username = u.username,
                                  AvatarId = u.UserProfile.avatarID
                              })
                        .ToList();

                    return friendsData;
                }
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(GetFriends)} - SQL Server Error");
                return new List<FriendData>();
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(GetFriends)} - Invalid Operation (DataBase Context)");
                return new List<FriendData>();
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetFriends));
                return new List<FriendData>();
            }
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username.ToLower() == username.ToLower());
                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    int currentUserId = user.userID;

                    var pendingRequests = context.Friendship
                        .Where(f => f.friendID == currentUserId && f.status == PENDING_STATUS)
                        .Join(context.User.Include("UserProfile"),
                              f => f.userID,
                              u => u.userID,
                              (f, u) => new FriendData
                              {
                                  Username = u.username,
                                  AvatarId = u.UserProfile.avatarID
                              })
                        .ToList();

                    return pendingRequests;
                }
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(GetFriends)} - SQL Server Error");
                return new List<FriendData>();
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(GetFriends)} - Invalid Operation (DataBase Context)");
                return new List<FriendData>();
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetFriends));
                return new List<FriendData>();
            }
        }

        public string CreateLobby(string hostUsername, int maxPlayers, string privacy)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var host = context.User.FirstOrDefault(u => u.username == hostUsername)
                        ?? throw new InvalidOperationException("Host user not found.");

                    int versionId = ResolveVersionId(context, maxPlayers);
                    string matchCode = GenerateMatchCode();

                    string normalizedStatus =
                        privacy.Equals("public", StringComparison.OrdinalIgnoreCase)
                        ? PUBLIC_STATUS : PRIVATE_STATUS;

                    var lobby = CreateNewLobby(context, host, versionId, maxPlayers, normalizedStatus);

                    AddLobbyOwner(context, lobby, host);
                    if (lobby.status.Equals(PRIVATE_STATUS, StringComparison.OrdinalIgnoreCase))
                    {
                        CreatePrivateInvitation(context, host, matchCode);
                    }

                    context.SaveChanges();
                    RegisterLobbyMapping(matchCode, lobby);

                    Console.WriteLine($"[SERVER] Lobby created by {hostUsername}, code={matchCode}, privacy={privacy}, maxPlayers={maxPlayers}.");
                    return matchCode;
                }
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(CreateLobby)} - Business Logic Error (Host not found)");
                return string.Empty;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(CreateLobby)} - DataBase Saving Error");
                return string.Empty;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(CreateLobby)} - SQL Server Error");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(CreateLobby));
                return string.Empty;
            }
        }

        public bool JoinMatch(string matchCode, string player)
        {
            bool joinSuccess = false;
            Lobby lobby = null;

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    lobby = ResolveLobbyForJoin(context, matchCode);
                    if (lobby == null || lobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[JOIN] Denied: Lobby closed or not found for code {matchCode}.");
                        return false;
                    }

                    bool isGuest = player.StartsWith("Guest_");

                    if (isGuest)
                    {
                        if (!lobby.status.Equals(PUBLIC_STATUS, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[JOIN] Denied: Guest player {player} tried to join a private lobby.");
                            return false;
                        }

                        int currentPlayers = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
                        if (currentPlayers >= lobby.maxPlayers)
                        {
                            Console.WriteLine($"[JOIN] Denied: Public lobby {lobby.lobbyID} is full ({currentPlayers}/{lobby.maxPlayers}).");
                            return false;
                        }

                        RegisterMatchCallback(matchCode, player);
                        NotifyPlayerJoined(matchCode, player);
                        Console.WriteLine($"[SERVER] Guest player {player} joined the public lobby {matchCode}.");
                        return true;
                    }

                    User playerUser = context.User.FirstOrDefault(u => u.username == player);

                    if (!ValidateJoinConditions(context, lobby, playerUser))
                    {
                        return false;
                    }

                    AddPlayerToLobby(context, lobby, playerUser);

                    joinSuccess = true;

                    if (joinSuccess)
                    {
                        RegisterMatchCallback(matchCode, player);
                        NotifyPlayerJoined(matchCode, player);
                        Console.WriteLine($"[SERVER] {player} joined the lobby {matchCode}.");
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(JoinMatch)} - DataBase Saving Error");
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(JoinMatch)} - SQL Server Error");
            }
            catch (CommunicationException ex)
            {
                LogError(ex, $"{nameof(JoinMatch)} - Callback Communication Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(JoinMatch));
            }

            return joinSuccess;
        }

        public void LeaveMatch(string matchCode, string player)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = ResolveLobbyForLeave(context, matchCode, player, out User username);
                    if (lobby == null || username == null)
                    {
                        return;
                    }

                    RemovePlayerFromLobby(context, lobby, username);
                    NotifyPlayerLeft(matchCode, player);
                    HandleEmptyLobbyCleanup(context, lobby, matchCode);
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(LeaveMatch)} - DataBase Deletion Error");
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(LeaveMatch)} - SQL Server Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(LeaveMatch));
            }
        }

        public void StartMatch(string matchCode)
        {
            try
            {
                var players = GetLobbyPlayers(matchCode);
                NotifyMatchStart(matchCode, players);
                HandleMatchStartupCleanup(matchCode);
            }
            catch (CommunicationException ex)
            {
                LogError(ex, $"{nameof(StartMatch)} - Callback Communication Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(StartMatch));
            }
        }

        public List<PlayerInfo> GetLobbyPlayers(string matchCode)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    Lobby lobby = FindLobbyByMatchCode(context, matchCode, true);
                    if (lobby == null)
                    {
                        return new List<PlayerInfo>();
                    }

                    var ownerUsername = context.User
                        .Where(u => u.userID == lobby.ownerID)
                        .Select(u => u.username)
                        .FirstOrDefault();

                    var dbPlayers = (from lm in context.LobbyMember
                                     join u in context.User on lm.userID equals u.userID
                                     join up in context.UserProfile on u.userID equals up.userID into upj
                                     from up in upj.DefaultIfEmpty()
                                     where lm.lobbyID == lobby.lobbyID
                                     select new PlayerInfo
                                     {
                                         Username = u.username,
                                         AvatarId = up != null ? up.avatarID : DEFAULT_AVATAR_ID,
                                         OwnerUsername = ownerUsername
                                     }).ToList();

                    if (matchCallbacks.TryGetValue(matchCode, out var callbacks))
                    {
                        var connectedPlayers = callbacks
                            .OfType<ITrucoCallback>()
                            .Select(cb => GetPlayerNameFromCallback(cb))
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Distinct()
                            .ToList();

                        var playersToAdd = connectedPlayers
                            .Where(connectedName => !dbPlayers.Any(p => p.Username == connectedName))
                            .Select(player => new PlayerInfo
                            {
                                Username = player,
                                AvatarId = DEFAULT_AVATAR_ID,
                                OwnerUsername = ownerUsername
                            });

                        dbPlayers.AddRange(playersToAdd);
                    }

                    return dbPlayers;
                }
            }
            catch (FormatException ex)
            {
                LogError(ex, $"{nameof(GetLobbyPlayers)} - Code Format Error");
                return new List<PlayerInfo>();
            }
            catch (OverflowException ex)
            {
                LogError(ex, $"{nameof(GetLobbyPlayers)} - Code Out of Range");
                return new List<PlayerInfo>();
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(GetLobbyPlayers)} - SQL Server Error");
                return new List<PlayerInfo>();
            }
            catch (NotSupportedException ex)
            {
                LogError(ex, $"{nameof(GetLobbyPlayers)} - LINQ Not Supported");
                return new List<PlayerInfo>();
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(GetLobbyPlayers)} - Invalid Operation (DataBase Context)");
                return new List<PlayerInfo>();
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetLobbyPlayers));
                return new List<PlayerInfo>();
            }
        }

        public List<PublicLobbyInfo> GetPublicLobbies()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var publicLobbiesQuery = context.Lobby
                        .Where(l => l.status == PUBLIC_STATUS)
                        .Select(l => new
                        {
                            l.lobbyID,
                            l.maxPlayers,
                            l.createdAt,
                            l.ownerID
                        })
                        .ToList();

                    var result = new List<PublicLobbyInfo>();

                    foreach (var lobby in publicLobbiesQuery)
                    {
                        int currentPlayers = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);
                        var owner = context.User.FirstOrDefault(u => u.userID == lobby.ownerID);

                        string matchCode = matchCodeToLobbyId.FirstOrDefault(kvp => kvp.Value == lobby.lobbyID).Key ?? "(unknown)";

                        result.Add(new PublicLobbyInfo
                        {
                            MatchCode = matchCode,
                            MatchName = $"{owner?.username ?? "Host"} - {lobby.maxPlayers}P",
                            CurrentPlayers = currentPlayers,
                            MaxPlayers = lobby.maxPlayers
                        });
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetPublicLobbies));
                return new List<PublicLobbyInfo>();
            }
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
                LogError(ex, $"{nameof(GetGlobalRanking)} - LINQ Not Supported");
                return new List<PlayerStats>();
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(GetGlobalRanking)} - SQL Error");
                return new List<PlayerStats>();
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(GetGlobalRanking)} - Invalid Operation (DataBase Context)");
                return new List<PlayerStats>();
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetGlobalRanking));
                return new List<PlayerStats>();
            }
        }

        public List<MatchResult> GetLastMatches(string username)
        {
            throw new NotImplementedException();
        }

        public List<string> GetOnlinePlayers()
        {
            throw new NotImplementedException();
        }

        public void JoinMatchChat(string matchCode, string player)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                RemoveInactiveCallbacks(matchCode);

                lock (matchCallbacks)
                {
                    if (!matchCallbacks.ContainsKey(matchCode))
                    {
                        matchCallbacks[matchCode] = new List<ITrucoCallback>();
                    }

                    if (!matchCallbacks[matchCode].Any(cb => ReferenceEquals(cb, callback)))
                    {
                        matchCallbacks[matchCode].Add(callback);
                    }
                }

                Console.WriteLine($"[CHAT] {player} joined the lobby {matchCode}.");
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(JoinMatchChat)} - There is no WCF Operational Context");
            }
            catch (OutOfMemoryException ex)
            {
                LogError(ex, $"{nameof(JoinMatchChat)} - Insuficient Memory");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(JoinMatchChat));
            }
        }

        public void LeaveMatchChat(string matchCode, string player)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                lock (matchCallbacks)
                {
                    if (matchCallbacks.ContainsKey(matchCode))
                    {
                        matchCallbacks[matchCode].Remove(callback);
                    }
                }

                if (matchCallbacks.ContainsKey(matchCode))
                {
                    BroadcastToMatchCallbacksAsync(matchCode, cb =>
                    {
                        try
                        {
                            cb.OnPlayerLeft(matchCode, player);
                        }
                        catch (CommunicationException ex)
                        {
                            LogError(ex, $"{nameof(LeaveMatchChat)} - Callback Communication Error");
                        }
                        catch (TimeoutException ex)
                        {
                            LogError(ex, $"{nameof(LeaveMatchChat)} - Timeout When Notifying Exit");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, nameof(LeaveMatchChat));
                        }
                    });
                }
                Console.WriteLine($"[CHAT] {player} left the lobby {matchCode}.");
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(LeaveMatchChat)} - There is no WCF Operational Context");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(LeaveMatchChat));
            }
        }

        public void SendChatMessage(string matchCode, string player, string message)
        {
            try
            {
                RemoveInactiveCallbacks(matchCode);

                if (!matchCallbacks.ContainsKey(matchCode))
                {
                    return;
                }

                var senderCallback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                BroadcastToMatchCallbacksAsync(matchCode, cb =>
                {
                    try
                    {
                        if (!ReferenceEquals(cb, senderCallback))
                        {
                            cb.OnChatMessage(matchCode, player, message);
                        }
                    }
                    catch (CommunicationException ex)
                    {
                        LogError(ex, $"{nameof(SendChatMessage)} - Disconnected Client");
                    }
                    catch (TimeoutException ex)
                    {
                        LogError(ex, $"{nameof(SendChatMessage)} - Timeout When Sending Message");
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, $"{nameof(SendChatMessage)} - Callback Error");
                    }
                });

                Console.WriteLine($"[{matchCode}] {player}: {message}");
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(SendChatMessage)} - Invalid OperationContext");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SendChatMessage));
            }
        }

        public void PlayCard(string matchCode, string player, string card)
        {
            throw new NotImplementedException();
        }

        private void ProcessSingleCallbackAsync(string matchCode, ITrucoCallback cb, Action<ITrucoCallback> invocation)
        {
            try
            {
                var comm = (ICommunicationObject)cb;

                if (comm.State != CommunicationState.Opened)
                {
                    lock (matchCallbacks)
                    {
                        RemoveInactiveCallbacks(matchCode);
                    }
                    try
                    {
                        comm.Abort();
                    }
                    catch 
                    { 
                        /* noop */ 
                    }

                    return;
                }

                invocation(cb);
            }
            catch (InvalidCastException ex)
            {
                LogError(ex, $"{nameof(ProcessSingleCallbackAsync)} - Invalid Callback Cast");
                lock (matchCallbacks)
                {
                    if (matchCallbacks.TryGetValue(matchCode, out var listLocal))
                    {
                        listLocal.Remove(cb);
                    }
                }
            }
            catch (SynchronizationLockException ex)
            {
                LogError(ex, $"{nameof(ProcessSingleCallbackAsync)} - Synchronization Block Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(BroadcastToMatchCallbacksAsync));
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
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                LogError(ex, $"{nameof(GenerateSecureNumericCode)} - Cryptographic Provider Error");
                return "000000";
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GenerateSecureNumericCode));
                return "000000";
            }
        }

        private static int ResolveVersionId(baseDatosTrucoEntities context, int maxPlayers)
        {
            try
            {
                int versionId = context.Versions
                    .Where(v => v.configuration.Contains(maxPlayers == 2 ? "1v1" : "2v2"))
                    .Select(v => v.versionID)
                    .FirstOrDefault();

                return versionId == 0 && context.Versions.Any()
                    ? context.Versions.First().versionID
                    : versionId;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(ResolveVersionId)} - SQL Server Error");
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(ResolveVersionId)} - Invalid Operation (DataBase Context)");
                return 0;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ResolveVersionId));
                return 0;
            }
        }

        private Lobby CreateNewLobby(baseDatosTrucoEntities context, User host, int versionId, int maxPlayers, string status)
        {
            try
            {
                var newLobby = new Lobby
                {
                    ownerID = host.userID,
                    versionID = versionId,
                    maxPlayers = maxPlayers,
                    status = status,
                    createdAt = DateTime.Now
                };

                context.Lobby.Add(newLobby);
                context.SaveChanges();

                return newLobby;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(CreateNewLobby)} - DataBase Saving Error");
                return null;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(CreateNewLobby)} - SQL Server Error");
                return null;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(CreateNewLobby));
                return null;
            }
        }

        private void AddLobbyOwner(baseDatosTrucoEntities context, Lobby lobby, User host)
        {
            try
            {
                context.LobbyMember.Add(new LobbyMember
                {
                    lobbyID = lobby.lobbyID,
                    userID = host.userID,
                    role = "Owner"
                });
            }
            catch (ArgumentNullException ex)
            {
                LogError(ex, $"{nameof(AddLobbyOwner)} - Null Argument");
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(AddLobbyOwner));
                throw;
            }
        }

        private void CreatePrivateInvitation(baseDatosTrucoEntities context, User host, string matchCode)
        {
            try
            {
                int numericCode = GenerateNumericCodeFromString(matchCode);

                var previousInvitations = context.Invitation
                    .Where(i => i.senderID == host.userID && i.status == PENDING_STATUS)
                    .ToList();

                foreach (var inv in previousInvitations)
                {
                    inv.status = EXPIRED_STATUS;
                    inv.expiresAt = DateTime.Now;
                }

                context.Invitation.Add(new Invitation
                {
                    senderID = host.userID,
                    receiverEmail = null,
                    code = numericCode,
                    status = PENDING_STATUS,
                    expiresAt = DateTime.Now.AddHours(2)
                });
            }
            catch (FormatException ex)
            {
                LogError(ex, $"{nameof(CreatePrivateInvitation)} - Code Format Error");
                throw;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(CreatePrivateInvitation)} - SQL Server Error on Query");
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(CreatePrivateInvitation));
                throw;
            }
        }

        private void RegisterLobbyMapping(string matchCode, Lobby lobby)
        {
            matchCodeToLobbyId[matchCode] = lobby.lobbyID;
            matchCallbacks.TryAdd(matchCode, new List<ITrucoCallback>());
        }

        private Lobby ResolveLobbyForJoin(baseDatosTrucoEntities context, string matchCode)
        {
            try
            {
                if (matchCodeToLobbyId.TryGetValue(matchCode, out int lobbyId))
                {
                    var lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == lobbyId);

                    if (lobby != null && !lobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
                    {
                        return lobby;
                    }

                    Console.WriteLine($"[SECURITY] Lobby {lobbyId} ({matchCode}) is closed or invalid.");
                    matchCodeToLobbyId.TryRemove(matchCode, out _);
                    return null;
                }

                int numericCode = GenerateNumericCodeFromString(matchCode);
                var invitation = context.Invitation.FirstOrDefault(i =>
                    i.code == numericCode &&
                    i.status == PENDING_STATUS &&
                    i.expiresAt > DateTime.Now);

                if (invitation == null)
                {
                    Console.WriteLine($"[SECURITY] No valid invitation found for {matchCode}.");
                    return null;
                }

                var lobbyCandidate = context.Lobby.FirstOrDefault(l =>
                    l.ownerID == invitation.senderID &&
                    !l.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase));

                if (lobbyCandidate == null)
                {
                    Console.WriteLine($"[SECURITY] Invitation {matchCode} points to closed or missing lobby.");
                    return null;
                }

                matchCodeToLobbyId.TryAdd(matchCode, lobbyCandidate.lobbyID);

                return lobbyCandidate;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(ResolveLobbyForJoin)} - SQL Server Error");
                return null;
            }
            catch (FormatException ex)
            {
                LogError(ex, $"{nameof(ResolveLobbyForJoin)} - Code Format Error");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(ResolveLobbyForJoin)} - Invalid Operation (DataBase Context)");
                return null;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ResolveLobbyForJoin));
                return null;
            }
        }

        private static bool ValidateJoinConditions(baseDatosTrucoEntities context, Lobby lobby, User playerUser)
        {
            if (playerUser == null)
            {
                Console.WriteLine($"[JOIN] Player not found.");
                return false;
            }

            if (lobby == null || lobby.status.Equals(CLOSED_STATUS, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[JOIN] Denied: Lobby {lobby.lobbyID} is closed.");
                return false;
            }

            try
            {
                int count = context.LobbyMember.Count(lm => lm.lobbyID == lobby.lobbyID);

                if (count >= lobby.maxPlayers)
                {
                    Console.WriteLine($"[JOIN] Lobby {lobby.lobbyID} is full ({count}/{lobby.maxPlayers}).");
                    return false;
                }

                return true;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(ValidateJoinConditions)} - SQL Server Error");
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ValidateJoinConditions));
                return false;
            }
        }

        private void AddPlayerToLobby(baseDatosTrucoEntities context, Lobby lobby, User playerUser)
        {
            try
            {
                if (!context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID && lm.userID == playerUser.userID))
                {
                    context.LobbyMember.Add(new LobbyMember
                    {
                        lobbyID = lobby.lobbyID,
                        userID = playerUser.userID,
                        role = "Player"
                    });

                    context.SaveChanges();
                    Console.WriteLine($"[JOIN] Player '{playerUser.username}' added to lobby {lobby.lobbyID}.");
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(AddPlayerToLobby)} - DataBase Saving Error");
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(AddPlayerToLobby)} - SQL Server Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(AddPlayerToLobby));
            }
        }

        private void RegisterMatchCallback(string matchCode, string player)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                RemoveInactiveCallbacks(matchCode);

                lock (matchCallbacks)
                {
                    if (!matchCallbacks.ContainsKey(matchCode))
                    {
                        matchCallbacks[matchCode] = new List<ITrucoCallback>();
                    }

                    if (!matchCallbacks[matchCode].Any(cb => ReferenceEquals(cb, callback)))
                    {
                        matchCallbacks[matchCode].Add(callback);
                        Console.WriteLine($"[JOIN] Callback added for {player} in match {matchCode}.");
                        matchCallbackToPlayerName[callback] = player;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(RegisterMatchCallback)} - No WCF Operational Context");
            }
            catch (SynchronizationLockException ex)
            {
                LogError(ex, $"{nameof(RegisterMatchCallback)} - Synchronization Block Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(RegisterMatchCallback));
            }
        }

        private void NotifyPlayerJoined(string matchCode, string player)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnPlayerJoined(matchCode, player);
                }
                catch (CommunicationException ex)
                {
                    LogError(ex, $"{nameof(NotifyPlayerJoined)} - Callback Communication Error");
                }
                catch (TimeoutException ex)
                {
                    LogError(ex, $"{nameof(NotifyPlayerJoined)} - Timeout Error");
                }
                catch (Exception ex)
                {
                    LogError(ex, nameof(NotifyPlayerJoined));
                }
            });
        }

        private Lobby ResolveLobbyForLeave(baseDatosTrucoEntities context, string matchCode, string username, out User player)
        {
            try
            {
                player = context.User.FirstOrDefault(u => u.username == username);

                if (player == null)
                {
                    return null;
                }

                return FindLobbyByMatchCode(context, matchCode, true);
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(ResolveLobbyForLeave)} - SQL Server Error");
                player = null;
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(ResolveLobbyForLeave)} - Invalid Operation (DataBase Context)");
                player = null;
                return null;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ResolveLobbyForLeave));
                player = null;
                return null;
            }
        }

        private static void RemovePlayerFromLobby(baseDatosTrucoEntities context, Lobby lobby, User player)
        {
            try
            {
                var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobby.lobbyID && lm.userID == player.userID);

                if (member != null)
                {
                    context.LobbyMember.Remove(member);
                    context.SaveChanges();
                    Console.WriteLine($"[LEAVE] Player '{player.username}' removed from lobby {lobby.lobbyID}.");
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(RemovePlayerFromLobby)} - SQL Server Error");
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(RemovePlayerFromLobby)} - Invalid Operation (DataBase Context)");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(RemovePlayerFromLobby));
            }
        }

        private void NotifyPlayerLeft(string matchCode, string username)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnPlayerLeft(matchCode, username);
                }
                catch (CommunicationException ex)
                {
                    LogError(ex, $"{nameof(NotifyPlayerJoined)} - Callback Communication Error");
                }
                catch (TimeoutException ex)
                {
                    LogError(ex, $"{nameof(NotifyPlayerJoined)} - Timeout Error");
                }
                catch (Exception ex)
                {
                    LogError(ex, nameof(NotifyPlayerJoined));
                }
            });
        }

        private void HandleEmptyLobbyCleanup(baseDatosTrucoEntities context, Lobby lobby, string matchCode)
        {
            try 
            { 
                bool isEmpty = !context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID);

                if (isEmpty)
                {
                    bool closed = CloseLobbyById(lobby.lobbyID);
                    bool expired = ExpireInvitationByMatchCode(matchCode);
                    bool removed = RemoveLobbyMembersById(lobby.lobbyID);

                    Console.WriteLine($"[CLEANUP] Lobby {lobby.lobbyID} cleanup result: closedLobby={closed}, expiredInvitation={expired}, removedMembers={removed}");
                    matchCodeToLobbyId.TryRemove(matchCode, out _);
                }
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(HandleEmptyLobbyCleanup)} - SQL Server Error on Check");
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(HandleEmptyLobbyCleanup)} - Invalid Operation (DataBase Context)");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(HandleEmptyLobbyCleanup));
            }
        }

        private void NotifyMatchStart(string matchCode, List<PlayerInfo> players)
        {
            BroadcastToMatchCallbacksAsync(matchCode, cb =>
            {
                try
                {
                    cb.OnMatchStarted(matchCode, players);
                }
                catch (CommunicationException ex)
                {
                    LogError(ex, $"{nameof(NotifyPlayerJoined)} - Callback Communication Error");
                }
                catch (TimeoutException ex)
                {
                    LogError(ex, $"{nameof(NotifyPlayerJoined)} - Timeout Error");
                }
                catch (Exception ex)
                {
                    LogError(ex, nameof(NotifyPlayerJoined));
                }
            });

            Console.WriteLine($"[SERVER] Match {matchCode} started by the owner.");
        }

        private void HandleMatchStartupCleanup(string matchCode)
        {
            if (!matchCodeToLobbyId.TryGetValue(matchCode, out int lobbyId))
            {
                Console.WriteLine($"[WARNING] No lobby found for {matchCode} during match start cleanup.");
                return;
            }

            try
            {
                bool closedLobby = CloseLobbyById(lobbyId);
                bool expiredInvitation = ExpireInvitationByMatchCode(matchCode);
                bool removedLobby = RemoveLobbyMembersById(lobbyId);

                if (!closedLobby || !expiredInvitation || !removedLobby)
                {
                    Console.WriteLine($"[WARNING] Partial DataBase update for {matchCode} (closedLobby={closedLobby}, expiredInvitation={expiredInvitation}, removedMembers={removedLobby}).");
                }
                else
                {
                    Console.WriteLine($"[SERVER] Lobby {matchCode} fully cleaned after starting match.");
                }

                matchCodeToLobbyId.TryRemove(matchCode, out _);
                Console.WriteLine($"[CLEANUP] Removed mapping for {matchCode}.");
            }
            catch (KeyNotFoundException ex)
            {
                LogError(ex, $"{nameof(HandleMatchStartupCleanup)} - Key Not Found");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(HandleMatchStartupCleanup));
            }
        }

        private static bool CloseLobbyById(int lobbyId)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == lobbyId);
                    if (lobby == null)
                    {
                        return false;
                    }

                    if (lobby.status != CLOSED_STATUS)
                    {
                        lobby.status = CLOSED_STATUS;
                        context.SaveChanges();
                    }
                    return true;
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                LogError(ex, $"{nameof(CloseLobbyById)} - Concurrency");
                return false;
            }
            catch (UpdateException ex)
            {
                LogError(ex, $"{nameof(CloseLobbyById)} - Update Error");
                return false;
            }
            catch (DbEntityValidationException ex)
            {
                LogError(ex, $"{nameof(CloseLobbyById)} - Entity Validation");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(CloseLobbyById)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(CloseLobbyById)} - SQL Error");
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(CloseLobbyById));
                return false;
            }
        }

        private static bool ExpireInvitationByMatchCode(string matchCode)
        {
            try
            {
                int numericCode = GenerateNumericCodeFromString(matchCode);
                using (var context = new baseDatosTrucoEntities())
                {
                    var invitations = context.Invitation
                        .Where(i => i.code == numericCode && i.status == PENDING_STATUS)
                        .ToList();

                    if (!invitations.Any())
                    {
                        return true;
                    }

                    foreach (var inv in invitations)
                    {
                        inv.status = EXPIRED_STATUS;
                        inv.expiresAt = DateTime.Now;
                    }

                    context.SaveChanges();
                    return true;
                }
            }
            catch (FormatException ex)
            {
                LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - Invalid Code");
                return false;
            }
            catch (OverflowException ex)
            {
                LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - Code Out of Range");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(ExpireInvitationByMatchCode)} - SQL Error");
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(ExpireInvitationByMatchCode));
                return false;
            }
        }

        private static bool RemoveLobbyMembersById(int lobbyId)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var members = context.LobbyMember.Where(lm => lm.lobbyID == lobbyId).ToList();
                    if (!members.Any())
                    {
                        return true;
                    }

                    context.LobbyMember.RemoveRange(members);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (UpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 547))
            {
                LogError(ex, $"{nameof(RemoveLobbyMembersById)} - FK Restriction (Code 547)");
                return false;
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, $"{nameof(RemoveLobbyMembersById)} - Database Deleting Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(RemoveLobbyMembersById)} - SQL Error");
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(RemoveLobbyMembersById));
                return false;
            }
        }

        private Lobby FindLobbyByMatchCode(baseDatosTrucoEntities context, string matchCode, bool onlyOpen = true)
        {
            try
            {
                int numericCode = GenerateNumericCodeFromString(matchCode);

                Lobby lobby = GetLobbyByMapping(context, matchCode, onlyOpen);
                if (lobby != null)
                {
                    return lobby;
                }

                var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);
                if (invitation == null)
                {
                    return null;
                }

                return GetLobbyByOwner(context, invitation.senderID, onlyOpen);
            }
            catch (SqlException ex)
            {
                LogError(ex, $"{nameof(FindLobbyByMatchCode)} - SQL Server Error");
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, $"{nameof(FindLobbyByMatchCode)} - Invalid Operation (Database Context/Mapping)");
            }
            catch (FormatException ex)
            {
                LogError(ex, $"{nameof(FindLobbyByMatchCode)} - Code Format Error");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(FindLobbyByMatchCode));
            }

            return null;
        }

        private Lobby GetLobbyByMapping(baseDatosTrucoEntities context, string matchCode, bool onlyOpen)
        {
            if (!matchCodeToLobbyId.TryGetValue(matchCode, out int mappedLobbyId))
            {
                return null;
            }

            IQueryable<Lobby> query = context.Lobby.Where(l => l.lobbyID == mappedLobbyId);

            if (onlyOpen)
            {
                query = query.Where(l => l.status == PUBLIC_STATUS || l.status == PRIVATE_STATUS);
            }

            return query.FirstOrDefault();
        }

        private static Lobby GetLobbyByOwner(baseDatosTrucoEntities context, int ownerId, bool onlyOpen)
        {
            IQueryable<Lobby> query = context.Lobby.Where(l => l.ownerID == ownerId);

            if (onlyOpen)
            {
                query = query.Where(l => l.status == PUBLIC_STATUS || l.status == PRIVATE_STATUS);
            }

            return query.FirstOrDefault();
        }

        private static string GetPlayerNameFromCallback(ITrucoCallback callback)
        {
            try
            {
                string name;
                if (matchCallbackToPlayerName.TryGetValue(callback, out name))
                {
                    return name;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
