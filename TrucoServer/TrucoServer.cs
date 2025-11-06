using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Langs;

namespace TrucoServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class TrucoServer : ITrucoUserService, ITrucoFriendService, ITrucoMatchService
    {
        private static readonly ConcurrentDictionary<string, string> VerificationCodes = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, ITrucoCallback> OnlineUsers = new ConcurrentDictionary<string, ITrucoCallback>();
        private readonly ConcurrentDictionary<string, int> matchCodeToLobbyId = new ConcurrentDictionary<string, int>();
        private const int MaxNameChanges = 2;

        private readonly ConcurrentDictionary<string, List<ITrucoCallback>> matchCallbacks = new ConcurrentDictionary<string, List<ITrucoCallback>>();

        private void LogError(Exception ex, string methodName)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR en {methodName}] {ex.GetType().Name}: {ex.Message}");
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
                if (OnlineUsers.TryGetValue(username, out ITrucoCallback callback))
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
                    OnlineUsers.TryRemove(username, out _);
                }
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Comunicación interrumpida para {username}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener callback de {username}: {ex.Message}");
            }
            return null;
        }

        private void BroadcastToMatchCallbacksAsync(string matchCode, Action<ITrucoCallback> invocation)
        {
            if (string.IsNullOrEmpty(matchCode) || invocation == null)
            {
                return;
            }

            if (!matchCallbacks.TryGetValue(matchCode, out var callbacksList))
            {
                return;
            }

            var snapshot = callbacksList.ToArray();

            foreach (var cb in snapshot)
            {
                Task.Run(() =>
                {
                    try
                    {
                        try
                        {
                            var comm = (ICommunicationObject)cb;
                            if (comm.State != CommunicationState.Opened)
                            {
                                lock (matchCallbacks)
                                {
                                    if (matchCallbacks.TryGetValue(matchCode, out var listLocal))
                                    {
                                        listLocal.Remove(cb);
                                    }
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
                        }
                        catch
                        {
                            
                        }

                        invocation(cb);
                    }
                    catch (CommunicationException)
                    {
                        lock (matchCallbacks)
                        {
                            if (matchCallbacks.TryGetValue(matchCode, out var listLocal))
                            {
                                listLocal.Remove(cb);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, nameof(BroadcastToMatchCallbacksAsync));
                    }
                });
            }
        }

        private void CleanupDisconnectedCallbacks(string matchCode)
        {
            if (!matchCallbacks.ContainsKey(matchCode))
                return;

            lock (matchCallbacks)
            {
                var list = matchCallbacks[matchCode];
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
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                });
            }
        }

        private void RemoveMatchMappingIfClosed(string matchCode)
        {
            if (matchCodeToLobbyId.TryGetValue(matchCode, out int lobbyId))
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == lobbyId);
                    if (lobby == null || lobby.status != "Open")
                    {
                        matchCodeToLobbyId.TryRemove(matchCode, out _);
                    }
                }
            }
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            try
            {
                string code = new Random().Next(100000, 999999).ToString();
                VerificationCodes[email] = code;
                Task.Run(() => SendVerificationEmail(email, code, languageCode));
                Console.WriteLine($"Código enviado a {email}: {code}");
                return true;
            }
            catch (ArgumentNullException ex)
            {
                LogError(ex, nameof(RequestEmailVerification));
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, nameof(RequestEmailVerification));
            }
            catch (SmtpException ex)
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
            if (VerificationCodes.TryGetValue(email, out string storedCode))
            {
                if (storedCode == code)
                {
                    VerificationCodes.TryRemove(email, out _);
                    return true;
                }
            }
            return false;
        }

        private void SendVerificationEmail(string email, string code, string languageCode)
        {
            try
            {
                LanguageManager.SetLanguage(languageCode);

                var settings = ConfigurationReader.EmailSettings;
                MailAddress fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
                MailAddress toAddress = new MailAddress(email);

                string fromPassword = settings.FromPassword;

                string subject = Lang.EmailVerificationSubject;
                string body = string.Format(Lang.EmailVerificationBody, code).Replace("\\n", Environment.NewLine);

                SmtpClient smtp = new SmtpClient
                {
                    Host = settings.SmtpHost,
                    Port = settings.SmtpPort,
                    EnableSsl = settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (MailMessage message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch (SmtpFailedRecipientException ex)
            {
                LogError(ex, nameof(SendVerificationEmail));
            }
            catch (SmtpException ex)
            {
                LogError(ex, nameof(SendVerificationEmail));
            }
            catch (FormatException ex)
            {
                LogError(ex, nameof(SendVerificationEmail));
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SendVerificationEmail));
            }
        }

        public bool Register(string newUsername, string password, string email)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (context.User.Any(u => u.email == email || u.username == newUsername))
                    {
                        return false;
                    }

                    User user = new User
                    {
                        username = newUsername,
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
                        avatarID = "avatar_aaa_default",
                        socialLinksJson = Encoding.UTF8.GetBytes("{}")
                    };
                    context.UserProfile.Add(profile);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogError(ex, nameof(Register));
            }
            catch (ArgumentException ex)
            {
                LogError(ex, nameof(Register));
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(Register));
            }
            return false;
        }

        public bool UsernameExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            using (var context = new baseDatosTrucoEntities())
            {
                return context.User.Any(u => u.username == username);
            }
        }

        public bool EmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            using (var context = new baseDatosTrucoEntities())
            {
                return context.User.Any(u => u.email == email);
            }
        }

        public UserProfileData GetUserProfile(string username)
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
                    AvatarId = user.UserProfile?.avatarID ?? "avatar_aaa_default",
                    NameChangeCount = user.nameChangeCount,
                    FacebookHandle = links?.facebook ?? "",
                    XHandle = links?.x ?? "",
                    InstagramHandle = links?.instagram ?? ""
                };
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
                        if (user.nameChangeCount >= MaxNameChanges)
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

                    user.UserProfile.avatarID = profile.AvatarId ?? "avatar_aaa_default";
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
            catch
            {
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
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<UserProfileData> GetUserProfileByEmailAsync(string email)
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
                    AvatarId = user.UserProfile?.avatarID ?? "avatar_aaa_default",
                    NameChangeCount = user.nameChangeCount,
                    FacebookHandle = links?.facebook ?? "",
                    XHandle = links?.x ?? "",
                    InstagramHandle = links?.instagram ?? ""
                };
            }
        }

        public bool PasswordReset(string email, string code, string newPassword, string languageCode) 
        { 
            if (!ConfirmEmailVerification(email, code)) 
            { 
                return false; 
            } 

            using (var context = new baseDatosTrucoEntities()) 
            { 
                User user = context.User.FirstOrDefault(u => u.email == email);
                
                if (user == null) 
                { 
                    return false; 
                } 

                user.passwordHash = PasswordHasher.Hash(newPassword); 
                context.SaveChanges(); 

                Task.Run(() => SendPasswordNotificationEmail(user.email, user.username, languageCode)); 
                return true; 
            } 
        }
        public bool PasswordChange(string email, string newPassword, string languageCode) 
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

                Task.Run(() => SendPasswordNotificationEmail(user.email, user.username, languageCode)); 
                return true; 
            } 
        }

        private void SendLoginNotificationEmail(string email, string username, string languageCode)
        {
            try
            {
                LanguageManager.SetLanguage(languageCode);

                var settings = ConfigurationReader.EmailSettings;
                MailAddress fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
                MailAddress toAddress = new MailAddress(email);
                string fromPassword = settings.FromPassword;

                string subject = Lang.EmailLoginNotificationSubject;
                string body = string.Format(Lang.EmailLoginNotificactionBody, username, DateTime.Now).Replace("\\n", Environment.NewLine);

                SmtpClient smtp = new SmtpClient
                {
                    Host = settings.SmtpHost,
                    Port = settings.SmtpPort,
                    EnableSsl = settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };
                using (MailMessage message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch (SmtpException ex)
            {
                LogError(ex, nameof(SendLoginNotificationEmail));
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SendLoginNotificationEmail));
            }
        }

        public bool Login(string username, string password, string languageCode)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                User user = context.User.FirstOrDefault(u => u.email == username || u.username == username);
                if (user != null && PasswordHasher.Verify(password, user.passwordHash))
                {
                    ITrucoCallback callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                    OnlineUsers.TryAdd(user.username, callback);

                    Task.Run(() => SendLoginNotificationEmail(user.email, user.username, languageCode));

                    return true;
                }
                return false;
            }
        }

        private void SendPasswordNotificationEmail(string email, string username, string languageCode) 
        { 
            try 
            { 
                LanguageManager.SetLanguage(languageCode); 

                var settings = ConfigurationReader.EmailSettings; 
                MailAddress fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName); 
                MailAddress toAddress = new MailAddress(email); 
                string fromPassword = settings.FromPassword; 

                string subject = Lang.EmailPasswordNotificationSubject; 
                string body = string.Format(Lang.EmailPasswordNotificationBody, username, DateTime.Now).Replace("\\n", Environment.NewLine); 

                SmtpClient smtp = new SmtpClient 
                { 
                    Host = settings.SmtpHost, 
                    Port = settings.SmtpPort, 
                    EnableSsl = settings.EnableSsl, 
                    DeliveryMethod = SmtpDeliveryMethod.Network, 
                    UseDefaultCredentials = false, 
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword) 
                }; 
                using (MailMessage message = new MailMessage(fromAddress, toAddress) 
                { 
                    Subject = subject, 
                    Body = body 
                }) 
                { 
                    smtp.Send(message); 
                } 
            } 
            catch (SmtpException ex) 
            { 
                LogError(ex, nameof(SendPasswordNotificationEmail)); 
            } 
            catch (Exception ex) 
            { 
                LogError(ex, nameof(SendPasswordNotificationEmail)); 
            } 
        }

        public bool SendFriendRequest(string fromUser, string toUser)
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
                    status = "Pending"
                };
                db.Friendship.Add(newRequest);
                db.SaveChanges();

                var targetUserCallback = GetUserCallback(toUser);
                targetUserCallback?.OnFriendRequestReceived(fromUser);

                return true;
            }
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
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
                    f.status == "Pending");
                if (request == null)
                {
                    return false;
                }

                request.status = "Accepted";

                Friendship reciprocalFriendship = new Friendship
                {
                    userID = acceptor.userID,
                    friendID = requester.userID,
                    status = "Accepted"
                };
                db.Friendship.Add(reciprocalFriendship);

                db.SaveChanges();

                var fromUserCallback = GetUserCallback(fromUser);
                fromUserCallback?.OnFriendRequestAccepted(toUser);

                return true;
            }
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
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

        public List<FriendData> GetFriends(string username)
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
                    .Where(f => (f.userID == currentUserId || f.friendID == currentUserId) && f.status == "Accepted")
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

        public List<FriendData> GetPendingFriendRequests(string username)
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
                    .Where(f => f.friendID == currentUserId && f.status == "Pending")
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

        public string CreateMatch(string hostPlayer)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var host = context.User.FirstOrDefault(u => u.username == hostPlayer);
                    if (host == null)
                    {
                        throw new InvalidOperationException("Usuario no encontrado.");
                    }

                    string matchCode = GenerateMatchCode();

                    int versionId = context.Versions.Any() ? context.Versions.First().versionID : 1;

                    Lobby lobby = new Lobby
                    {
                        ownerID = host.userID,
                        versionID = versionId,
                        maxPlayers = 4,
                        status = "Open",
                        createdAt = DateTime.Now
                    };
                    context.Lobby.Add(lobby);
                    context.SaveChanges();

                    context.LobbyMember.Add(new LobbyMember
                    {
                        lobbyID = lobby.lobbyID,
                        userID = host.userID,
                        role = "Owner"
                    });

                    int numericCode = GenerateNumericCodeFromString(matchCode);
                    context.Invitation.Add(new Invitation
                    {
                        senderID = host.userID,
                        receiverEmail = null,
                        code = numericCode,
                        status = "Pending",
                        expiresAt = DateTime.Now.AddDays(1)
                    });

                    context.SaveChanges();

                    var previousInvitations = context.Invitation.Where(i => i.senderID == host.userID && i.status == "Pending").ToList();
                    foreach (var inv in previousInvitations)
                    {
                        if (inv.code != numericCode)
                        {
                            inv.status = "Expired";
                            inv.expiresAt = DateTime.Now;
                        }
                    }
                    context.SaveChanges();

                    matchCodeToLobbyId[matchCode] = lobby.lobbyID;

                    matchCallbacks.TryAdd(matchCode, new List<ITrucoCallback>());

                    Console.WriteLine($"[SERVER] Lobby privado creado por {hostPlayer} con código {matchCode}");
                    return matchCode;
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Console.WriteLine($"[VALIDATION ERROR] {validationError.PropertyName}: {validationError.ErrorMessage}");
                    }
                }
                LogError(ex, nameof(CreateMatch));
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, nameof(CreateMatch));
                return null;
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                LogError(ex, nameof(CreateMatch));
                if (ex.InnerException != null)
                    Console.WriteLine($"[INNER EXCEPTION] {ex.InnerException.Message}");
                return null;
            }
            catch (CommunicationException ex)
            {
                LogError(ex, nameof(CreateMatch));
                return null;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(CreateMatch));
                return null;
            }
        }

        public bool JoinMatch(string matchCode, string player)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    int numericCode = GenerateNumericCodeFromString(matchCode);

                    Lobby lobby = null;
                    if (matchCodeToLobbyId.TryGetValue(matchCode, out int mappedLobbyId))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == mappedLobbyId && l.status == "Open");
                        Console.WriteLine(lobby != null
                            ? $"[JOIN] Found lobby by mapping: {matchCode} -> {mappedLobbyId}"
                            : $"[JOIN] Mapping existed but no open lobby found for id {mappedLobbyId}");
                    }

                    Invitation invitation = null;
                    if (lobby == null)
                    {
                        invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode && i.status == "Pending");
                        if (invitation == null)
                        {
                            Console.WriteLine($"[JOIN] Invitation not found (code {numericCode}) for matchCode {matchCode}");
                            return false;
                        }

                        if (invitation.expiresAt != null && invitation.expiresAt.Date < DateTime.Now)
                        {
                            Console.WriteLine($"[JOIN] Invitation expired for code {numericCode}");
                            return false;
                        }

                        lobby = context.Lobby.FirstOrDefault(l => l.ownerID == invitation.senderID && l.status == "Open");
                        if (lobby == null)
                        {
                            Console.WriteLine($"[JOIN] Lobby not found for invitation sender {invitation.senderID}");
                            return false;
                        }

                        matchCodeToLobbyId.TryAdd(matchCode, lobby.lobbyID);
                        Console.WriteLine($"[JOIN] Mapped {matchCode} -> lobbyId {lobby.lobbyID}");
                    }

                    var playerUser = context.User.FirstOrDefault(u => u.username == player);
                    if (playerUser == null)
                    {
                        Console.WriteLine($"[JOIN] Player user not found: {player}");
                        return false;
                    }

                    if (!context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID && lm.userID == playerUser.userID))
                    {
                        context.LobbyMember.Add(new LobbyMember
                        {
                            lobbyID = lobby.lobbyID,
                            userID = playerUser.userID,
                            role = "Player"
                        });

                        if (invitation != null)
                        {
                            invitation.status = "Accepted";
                        }

                        context.SaveChanges();
                        Console.WriteLine($"[JOIN] Added LobbyMember user={player} to lobbyId={lobby.lobbyID}");
                    }
                    else
                    {
                        Console.WriteLine($"[JOIN] User {player} already a member of lobbyId={lobby.lobbyID}");
                    }

                    matchCallbacks.TryAdd(matchCode, new List<ITrucoCallback>());

                    var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();

                    string newSessionId = null;
                    try
                    {
                        var newCtx = callback as IContextChannel;
                        newSessionId = newCtx?.SessionId;
                    }
                    catch
                    {
                        newSessionId = null;
                    }

                    lock (matchCallbacks)
                    {
                        matchCallbacks[matchCode].RemoveAll(cb =>
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
                                return false;
                            }
                            catch
                            {
                                return true;
                            }
                        });

                        bool alreadyExists = false;
                        if (!string.IsNullOrEmpty(newSessionId))
                        {
                            foreach (var cb in matchCallbacks[matchCode])
                            {
                                try
                                {
                                    var ctx = cb as IContextChannel;
                                    if (ctx != null && ctx.SessionId == newSessionId)
                                    {
                                        alreadyExists = true;
                                        break;
                                    }
                                }
                                catch 
                                { 
                                    /* noop */ 
                                }
                            }
                        }
                        else
                        {
                            alreadyExists = matchCallbacks[matchCode].Any(cb => ReferenceEquals(cb, callback) || cb.GetHashCode() == callback.GetHashCode());
                        }

                        if (!alreadyExists)
                        {
                            matchCallbacks[matchCode].Add(callback);
                            Console.WriteLine($"[JOIN] Callback added for {player} on match {matchCode}");
                        }
                    }

                    BroadcastToMatchCallbacksAsync(matchCode, cb =>
                    {
                        try
                        {
                            cb.OnPlayerJoined(matchCode, player);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, nameof(JoinMatch));
                        }
                    });

                    Console.WriteLine($"[SERVER] {player} se unió al lobby {matchCode}");
                    return true;
                }
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, nameof(JoinMatch));
                return false;
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                LogError(ex, nameof(JoinMatch));
                return false;
            }
            catch (CommunicationException ex)
            {
                LogError(ex, nameof(JoinMatch));
                return false;
            }
        }

        public void JoinMatchChat(string matchCode, string player)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                CleanupDisconnectedCallbacks(matchCode);

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

                Console.WriteLine($"{player} se unió a {matchCode}");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(JoinMatchChat));
            }
        }

        public void LeaveMatch(string matchCode, string username)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    int numericCode = GenerateNumericCodeFromString(matchCode);

                    var player = context.User.FirstOrDefault(u => u.username == username);
                    if (player == null)
                    {
                        return;
                    }

                    Lobby lobby = null;

                    if (matchCodeToLobbyId.TryGetValue(matchCode, out int mappedLobbyId))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == mappedLobbyId && l.status == "Open");
                    }

                    if (lobby == null)
                    {
                        numericCode = GenerateNumericCodeFromString(matchCode);
                        var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);

                        if (invitation != null)
                        {
                            lobby = context.Lobby.FirstOrDefault(l => l.ownerID == invitation.senderID && l.status == "Open");
                        }
                    }

                    if (lobby == null)
                    {
                        Console.WriteLine($"[LEAVE] No se encontró lobby abierto para {matchCode}");
                        return;
                    }


                    var member = context.LobbyMember.FirstOrDefault(lm => lm.lobbyID == lobby.lobbyID && lm.userID == player.userID);
                    if (member != null)
                    {
                        context.LobbyMember.Remove(member);
                        context.SaveChanges();
                    }

                    BroadcastToMatchCallbacksAsync(matchCode, cb =>
                    {
                        try
                        {
                            cb.OnPlayerLeft(matchCode, username);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, nameof(LeaveMatch));
                        }
                    });

                    bool isEmpty = !context.LobbyMember.Any(lm => lm.lobbyID == lobby.lobbyID);
                    if (isEmpty)
                    {
                        lobby.status = "Closed";

                        var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);
                        if (invitation != null)
                        {
                            invitation.status = "Expired";
                            invitation.expiresAt = DateTime.Now;
                        }

                        context.SaveChanges();
                        Console.WriteLine($"[SERVER] Lobby {matchCode} expiró (vacío).");

                        RemoveMatchMappingIfClosed(matchCode);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(LeaveMatch));
            }
        }

        public List<PlayerInfo> GetLobbyPlayers(string matchCode)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    int numericCode = GenerateNumericCodeFromString(matchCode);

                    Lobby lobby = null;

                    if (matchCodeToLobbyId.TryGetValue(matchCode, out int mappedLobbyId))
                    {
                        lobby = context.Lobby.FirstOrDefault(l => l.lobbyID == mappedLobbyId && l.status == "Open");
                    }

                    if (lobby == null)
                    {
                        var invitation = context.Invitation.FirstOrDefault(i => i.code == numericCode);
                        if (invitation == null)
                        {
                            return new List<PlayerInfo>();
                        }

                        lobby = context.Lobby.FirstOrDefault(l => l.ownerID == invitation.senderID && l.status == "Open");
                        if (lobby == null)
                        {
                            return new List<PlayerInfo>();
                        }
                    }

                    var ownerUsername = context.User
                        .Where(u => u.userID == lobby.ownerID)
                        .Select(u => u.username)
                        .FirstOrDefault();

                    var players = (from lm in context.LobbyMember
                                   join u in context.User on lm.userID equals u.userID
                                   join up in context.UserProfile on u.userID equals up.userID into upj
                                   from up in upj.DefaultIfEmpty()
                                   where lm.lobbyID == lobby.lobbyID
                                   select new PlayerInfo
                                   {
                                       Username = u.username,
                                       AvatarId = (up != null ? up.avatarID : "avatar_aaa_default"),
                                       OwnerUsername = ownerUsername
                                   }).ToList();

                    return players;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetLobbyPlayers));
                return new List<PlayerInfo>();
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
                        catch (Exception ex)
                        {
                            LogError(ex, nameof(LeaveMatchChat));
                        }
                    });
                }
                Console.WriteLine($"{player} salió de {matchCode}");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(LeaveMatchChat));
            }
        }

        public void PlayCard(string matchCode, string player, string card)
        {
            throw new NotImplementedException();
        }

        public void StartMatch(string matchCode)
        {
            try
            {
                var players = GetLobbyPlayers(matchCode);

                BroadcastToMatchCallbacksAsync(matchCode, cb =>
                {
                    try
                    {
                        cb.OnMatchStarted(matchCode, players);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, nameof(StartMatch));
                    }
                });

                Console.WriteLine($"[SERVER] Partida {matchCode} iniciada por el propietario.");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(StartMatch));
            }
        }

        public void SendChatMessage(string matchCode, string player, string message)
        {
            try
            {
                CleanupDisconnectedCallbacks(matchCode);

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
                    catch (Exception ex)
                    {
                        LogError(ex, nameof(SendChatMessage));
                    }
                });


                Console.WriteLine($"[{matchCode}] {player}: {message}");
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(SendChatMessage));
            }
        }


        public List<PlayerStats> GetGlobalRanking()
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

        public List<MatchResult> GetLastMatches(string username)
        {
            throw new NotImplementedException();
        }

        public List<string> GetOnlinePlayers()
        {
            throw new NotImplementedException();
        }

        private static string GenerateMatchCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
    }
}
