using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer;
using TrucoServer.Langs;

namespace TrucoServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public partial class TrucoServer : ITrucoUserService
    {
        private static ConcurrentDictionary<string, string> verificationCodes = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ITrucoCallback> onlineUsers = new ConcurrentDictionary<string, ITrucoCallback>();
        private const int MAX_CHANGES = 2;

        private static ITrucoCallback GetUserCallback(string username)
        {
            if (onlineUsers.TryGetValue(username, out ITrucoCallback callback))
            {
                if (((System.ServiceModel.ICommunicationObject)callback).State == System.ServiceModel.CommunicationState.Opened)
                {
                    return callback;
                }
                else
                {
                    ((System.ServiceModel.ICommunicationObject)callback).Abort();
                    onlineUsers.TryRemove(username, out _);
                }
            }
            return null;
        }
        public bool RequestEmailVerification(string email, string languageCode)
        {
            try
            {
                string code = new Random().Next(100000, 999999).ToString();
                verificationCodes[email] = code;

                SendVerificationEmail(email, code, languageCode);
                Console.WriteLine($"Código enviado a {email}: {code}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando email: {ex.Message}");
                return false;
            }
        }

        public bool ConfirmEmailVerification(string email, string code)
        {
            if (verificationCodes.TryGetValue(email, out string storedCode))
            {
                if (storedCode == code)
                {
                    verificationCodes.TryRemove(email, out _);
                    return true;
                }
            }
            return false;
        }

        private void SendVerificationEmail(string email, string code, string languageCode)
        {
            LanguageManager.SetLanguage(languageCode);

            var fromAddress = new MailAddress("trucoargentinotcs@gmail.com", "Truco Argentino");
            var toAddress = new MailAddress(email);
            const string fromPassword = "obbw ipgm klkt tdxa";
            string subject = Lang.EmailVerificationSubject;
            string body = string.Format(Lang.EmailVerificationBody, code).Replace("\\n", Environment.NewLine);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        public bool Register(string username, string password, string email)
        {
            try
            {
                using (var context = new baseDatosPruebaEntities())
                {
                    bool existsEmail = context.User.Any(u => u.email == email);
                    bool existsUsername = context.User.Any(u => u.nickname == username);

                    if (existsEmail || existsUsername)
                    {
                        return false;
                    }

                    string hashedPassword = PasswordHasher.Hash(password);

                    User user = new User
                    {
                        nickname = username,
                        passwordHash = hashedPassword,
                        email = email,
                        wins = 0,
                        nameChangeCount = 0
                    };

                    context.User.Add(user);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al registrar usuario: " + ex.Message);
                return false;
            }
        }

        public UserProfileData GetUserProfile(string username)
        {
            using (var context = new baseDatosPruebaEntities())
            {
                var user = context.User
                                    .Include(u => u.UserProfile)
                                    .FirstOrDefault(u => u.nickname == username);

                if (user == null)
                {
                    return null;
                }
                var socialLinksText = (user.UserProfile?.socialLinksJson != null)
                    ? System.Text.Encoding.UTF8.GetString(user.UserProfile.socialLinksJson)
                    : "{}";

                dynamic socialLinks = Newtonsoft.Json.JsonConvert.DeserializeObject(socialLinksText);

                UserProfileData profile = new UserProfileData
                {
                    Username = user.nickname,
                    Email = user.email,
                    FacebookHandle = socialLinks.Facebook ?? string.Empty,
                    XHandle = socialLinks.X ?? string.Empty,
                    InstagramHandle = socialLinks.Instagram ?? string.Empty,
                    NameChangeCount = user.nameChangeCount,
                    AvatarId = user.UserProfile?.avatarID ?? "avatar_default"
                };

                return profile;
            }
        }

        public bool SaveUserProfile(UserProfileData profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Email))
                return false;

            try
            {
                using (var context = new baseDatosPruebaEntities())
                {
                    var userRecord = context.User
                        .Include(u => u.UserProfile)
                        .SingleOrDefault(u => u.email == profile.Email);

                    if (userRecord == null)
                        return false;

                    if (userRecord.nickname != profile.Username)
                    {
                        const int MAX_CHANGES = 2;

                        if (userRecord.nameChangeCount >= MAX_CHANGES)
                            return false;

                        bool nicknameExists = context.User
                            .Any(u => u.nickname == profile.Username && u.userID != userRecord.userID);

                        if (nicknameExists)
                            return false;

                        userRecord.nickname = profile.Username;
                        userRecord.nameChangeCount++;
                    }

                    if (userRecord.UserProfile == null)
                    {
                        userRecord.UserProfile = new UserProfile
                        {
                            userID = userRecord.userID
                        };
                        context.UserProfile.Add(userRecord.UserProfile);
                    }

                    var userProfileRecord = userRecord.UserProfile;
                    userProfileRecord.avatarID = profile.AvatarId ?? "avatar_default";
                    var socialLinks = new
                    {
                        facebook = profile.FacebookHandle ?? "",
                        x = profile.XHandle ?? "",
                        instagram = profile.InstagramHandle ?? ""
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(socialLinks);
                    userProfileRecord.socialLinksJson = System.Text.Encoding.UTF8.GetBytes(json);

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar perfil para {profile.Email}: {ex.Message}");
                return false;
            }
        }

        public Task<bool> UpdateUserAvatarAsync(string username, string newAvatarId)
        {
            try
            {
                using (var context = new baseDatosPruebaEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.nickname == username);
                    if (user == null)
                    {
                        return Task.FromResult(false);
                    }

                    var profile = context.UserProfile.FirstOrDefault(p => p.userID == user.userID);

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando avatar para {username}: {ex.Message}");
                return Task.FromResult(false);
            }
        }
        private void SendLoginNotificationEmail(string email, string nickname, string languageCode)
        {
            try
            {
                LanguageManager.SetLanguage(languageCode);

                var fromAddress = new MailAddress("trucoargentinotcs@gmail.com", "Truco Argentino");
                var toAddress = new MailAddress(email);
                const string fromPassword = "obbw ipgm klkt tdxa";
                string subject = Lang.EmailLoginNotificationSubject;
                string body = string.Format(Lang.EmailLoginNotificactionBody, nickname, DateTime.Now).Replace("\\n", Environment.NewLine);

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando notificación de login a {email}: {ex.Message}");
            }
        }
        public bool Login(string username, string password, string languageCode)
        {
            using (var context = new baseDatosPruebaEntities())
            {
                var user = context.User
                    .FirstOrDefault(u => u.email == username || u.nickname == username);

                if (user != null)
                {
                    if (!string.IsNullOrEmpty(user.passwordHash) && PasswordHasher.Verify(password, user.passwordHash))
                    {
                        ITrucoCallback callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                        onlineUsers.TryAdd(user.nickname, callback);
                        SendLoginNotificationEmail(user.email, user.nickname, languageCode);
                        return true;
                    }
                }

                return false;
            }
        }

        public bool PasswordReset(string email, string code, string newPassword)
        {
            try
            {
                bool valid = ConfirmEmailVerification(email, code);
                if (!valid)
                {
                    Console.WriteLine($"Código incorrecto o expirado para {email}");
                    return false;
                }

                using (var context = new baseDatosPruebaEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.email == email);
                    if (user == null)
                    {
                        Console.WriteLine($"No se encontró usuario con correo {email}");
                        return false;
                    }

                    string hashedPassword = PasswordHasher.Hash(newPassword);
                    user.passwordHash = hashedPassword;

                    context.SaveChanges();
                    Console.WriteLine($"Contraseña actualizada correctamente para {email}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al restablecer contraseña para {email}: {ex.Message}");
                return false;
            }
        }

        public void Logout(string username)
        {
            onlineUsers.TryRemove(username, out _);
        }

        public List<PlayerStats> GetGlobalRanking()
        {
            throw new NotImplementedException();
        }

        public List<MatchResult> GetLastMatches(string username)
        {
            throw new NotImplementedException();
        }

        public List<string> GetOnlinePlayers()
        {
            throw new NotImplementedException();
        }
    }

    public partial class TrucoServer : ITrucoFriendService
    {
        private FriendData GetFriendData(string nickname, baseDatosPruebaEntities db)
        {
            var user = db.User.AsNoTracking().FirstOrDefault(u =>
                EfHelpers.GetPropValue<string>(u, "nickname", "NickName", "nick") == nickname);

            if (user == null) return null;

            var profileObj = EfHelpers.GetNavigation(user, "UserProfile", "Profile", "userProfile");

            string avatarId = EfHelpers.GetPropValue<string>(profileObj, "avatarID", "avatarId", "AvatarID", "AvatarId")
                              ?? "avatar_default";

            return new FriendData
            {
                Username = EfHelpers.GetPropValue<string>(user, "nickname", "NickName"),
                AvatarId = avatarId
            };
        }

        private UserProfileData GetUserProfileData(string nickname, baseDatosPruebaEntities db)
        {
            var user = db.User.AsNoTracking().FirstOrDefault(u =>
                EfHelpers.GetPropValue<string>(u, "nickname", "NickName", "nick") == nickname);

            if (user == null) return null;

            var profileObj = EfHelpers.GetNavigation(user, "UserProfile", "Profile", "userProfile");

            return new UserProfileData
            {
                Username = EfHelpers.GetPropValue<string>(user, "nickname", "NickName"),
                Email = EfHelpers.GetPropValue<string>(user, "email", "Email", "mail"),
                AvatarId = EfHelpers.GetPropValue<string>(profileObj, "avatarID", "avatarId", "AvatarID") ?? "avatar_default",
                NameChangeCount = EfHelpers.GetPropValue<int>(user, "nameChangeCount", "NameChangeCount", "name_changes"),
                FacebookHandle = EfHelpers.GetPropValue<string>(profileObj, "facebookHandle", "FacebookHandle", "facebook"),
                XHandle = EfHelpers.GetPropValue<string>(profileObj, "xHandle", "XHandle", "x"),
                InstagramHandle = EfHelpers.GetPropValue<string>(profileObj, "instagramHandle", "InstagramHandle", "instagram"),
                EmblemLayers = new List<EmblemLayer>()
            };
        }

        public List<FriendData> GetFriends(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var users = db.User.AsNoTracking().ToList();
                var friendships = db.Friendship.AsNoTracking().ToList();

                var userObj = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == username);
                if (userObj == null) return new List<FriendData>();

                int userId = EfHelpers.GetPropValue<int>(userObj, "userID", "UserID", "Id");

                Func<object, int> getUID1 = f => EfHelpers.GetPropValue<int>(f, "userID1", "userID", "UserID", "userID_1", "UserId1");
                Func<object, int> getUID2 = f => EfHelpers.GetPropValue<int>(f, "userID2", "friendID", "userID_2", "UserId2", "FriendID");

                Func<object, string> getStatus = f => EfHelpers.GetPropValue<string>(f, "status", "Status", "estado");

                var accepted = friendships.Where(f =>
                    (getUID1(f) == userId || getUID2(f) == userId) &&
                    string.Equals(getStatus(f), "Accepted", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var friendIds = accepted.Select(f => getUID1(f) == userId ? getUID2(f) : getUID1(f)).Distinct().ToList();

                var friendNicknames = users.Where(u => friendIds.Contains(EfHelpers.GetPropValue<int>(u, "userID", "UserID", "Id")))
                                           .Select(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName"))
                                           .Where(n => !string.IsNullOrEmpty(n))
                                           .ToList();

                var friendsData = friendNicknames.Select(n => GetFriendData(n, db)).Where(fd => fd != null).ToList();
                return friendsData;
            }
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var users = db.User.AsNoTracking().ToList();
                var friendships = db.Friendship.AsNoTracking().ToList();

                var userObj = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == username);
                if (userObj == null) return new List<FriendData>();
                int userId = EfHelpers.GetPropValue<int>(userObj, "userID", "UserID", "Id");

                Func<object, int> getUID1 = f => EfHelpers.GetPropValue<int>(f, "userID1", "userID", "UserID", "userID_1", "UserId1");
                Func<object, int> getUID2 = f => EfHelpers.GetPropValue<int>(f, "userID2", "friendID", "userID_2", "UserId2", "FriendID");
                Func<object, string> getStatus = f => EfHelpers.GetPropValue<string>(f, "status", "Status", "estado");

                var pending = friendships.Where(f =>
                    getUID2(f) == userId &&
                    string.Equals(getStatus(f), "Pending", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var requesterIds = pending.Select(f => getUID1(f)).Distinct().ToList();

                var requesterNicknames = users.Where(u => requesterIds.Contains(EfHelpers.GetPropValue<int>(u, "userID", "UserID", "Id")))
                                             .Select(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName"))
                                             .Where(n => !string.IsNullOrEmpty(n))
                                             .ToList();

                var requestsData = requesterNicknames.Select(n => GetFriendData(n, db)).Where(fd => fd != null).ToList();
                return requestsData;
            }
        }
        public bool SendFriendRequest(string fromUser, string toUser)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var users = db.User.ToList();
                var sender = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == fromUser);
                var receiver = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == toUser);

                if (sender == null || receiver == null || EfHelpers.GetPropValue<int>(sender, "userID", "UserID") == EfHelpers.GetPropValue<int>(receiver, "userID"))
                    return false;

                var existing = db.Friendship
                    .AsNoTracking()
                    .ToList()
                    .FirstOrDefault(f =>
                    {
                        int a = EfHelpers.GetPropValue<int>(f, "userID1", "userID", "UserID");
                        int b = EfHelpers.GetPropValue<int>(f, "userID2", "friendID", "UserID");
                        return (a == EfHelpers.GetPropValue<int>(sender, "userID", "UserID") && b == EfHelpers.GetPropValue<int>(receiver, "userID", "UserID"))
                               || (a == EfHelpers.GetPropValue<int>(receiver, "userID", "UserID") && b == EfHelpers.GetPropValue<int>(sender, "userID", "UserID"));
                    });

                if (existing != null) return false;

                var newRel = Activator.CreateInstance(typeof(Friendship));
                var setProp = new Action<object, string, object>((obj, propName, val) =>
                {
                    var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (p != null && p.CanWrite) p.SetValue(obj, val);
                });

                setProp(newRel, "userID1", EfHelpers.GetPropValue<int>(sender, "userID", "UserID"));
                setProp(newRel, "userID2", EfHelpers.GetPropValue<int>(receiver, "userID", "UserID"));
                setProp(newRel, "status", "Pending");
                setProp(newRel, "requestDate", DateTime.UtcNow);

                db.Friendship.Add((Friendship)newRel);
                db.SaveChanges();
                return true;
            }
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var users = db.User.ToList();
                var requester = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == fromUser);
                var acceptor = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == toUser);
                if (requester == null || acceptor == null) return false;

                var all = db.Friendship.ToList();
                var request = all.FirstOrDefault(f =>
                    EfHelpers.GetPropValue<int>(f, "userID1", "userID") == EfHelpers.GetPropValue<int>(requester, "userID")
                    && EfHelpers.GetPropValue<int>(f, "userID2", "friendID") == EfHelpers.GetPropValue<int>(acceptor, "userID")
                    && string.Equals(EfHelpers.GetPropValue<string>(f, "status", "Status"), "Pending", StringComparison.OrdinalIgnoreCase)
                );

                if (request == null) return false;

                var prop = request.GetType().GetProperty("status", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite) prop.SetValue(request, "Accepted");

                db.Entry(request).State = EntityState.Modified;
                db.SaveChanges();
                return true;
            }
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var users = db.User.ToList();
                var u1 = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == user1);
                var u2 = users.FirstOrDefault(u => EfHelpers.GetPropValue<string>(u, "nickname", "NickName") == user2);
                if (u1 == null || u2 == null) return false;

                var all = db.Friendship.ToList();

                var toRemove = all.Where(f =>
                    (EfHelpers.GetPropValue<int>(f, "userID1", "userID") == EfHelpers.GetPropValue<int>(u1, "userID")
                     && EfHelpers.GetPropValue<int>(f, "userID2", "userID", "friendID") == EfHelpers.GetPropValue<int>(u2, "userID"))
                    ||
                    (EfHelpers.GetPropValue<int>(f, "userID1", "userID") == EfHelpers.GetPropValue<int>(u2, "userID")
                     && EfHelpers.GetPropValue<int>(f, "userID2", "userID", "friendID") == EfHelpers.GetPropValue<int>(u1, "userID"))
                ).ToList();

                if (!toRemove.Any()) return false;
                db.Friendship.RemoveRange(toRemove);
                db.SaveChanges();
                return true;
            }
        }

        public async Task<List<UserProfileData>> GetAcceptedFriendsDataAsync(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var user = db.User.FirstOrDefault(u => u.nickname == username);
                if (user == null)
                    return new List<UserProfileData>();

                var friends = db.Friendship
                    .Where(f =>
                        (f.userID == user.userID || f.friendID == user.userID)
                        && f.status == "Accepted")
                    .Select(f => f.userID == user.userID ? f.Friend : f.User)
                    .ToList();

                var profiles = new List<UserProfileData>();

                foreach (var friend in friends)
                {
                    var profile = friend.UserProfile;
                    if (profile == null)
                        continue;

                    string json = profile.socialLinksJson != null
                        ? System.Text.Encoding.UTF8.GetString(profile.socialLinksJson)
                        : "{}";

                    dynamic links = null;
                    try
                    {
                        links = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    }
                    catch
                    {
                        links = new { facebook = "", x = "", instagram = "" };
                    }

                    profiles.Add(new UserProfileData
                    {
                        Username = friend.nickname,
                        Email = friend.email,
                        AvatarId = profile.avatarID ?? "avatar_default",
                        NameChangeCount = friend.nameChangeCount,
                        FacebookHandle = links?.facebook ?? "",
                        XHandle = links?.x ?? "",
                        InstagramHandle = links?.instagram ?? "",
                        EmblemLayers = new List<EmblemLayer>()
                    });
                }

                return await Task.FromResult(profiles);
            }
        }

        public async Task<List<UserProfileData>> GetPendingFriendRequestsDataAsync(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var user = db.User.FirstOrDefault(u => u.nickname == username);
                if (user == null)
                    return new List<UserProfileData>();

                var pendingRequests = db.Friendship
                    .Where(f => f.friendID == user.userID && f.status == "Pending")
                    .Select(f => f.User)
                    .ToList();

                var profiles = new List<UserProfileData>();

                foreach (var requester in pendingRequests)
                {
                    var profile = requester.UserProfile;
                    if (profile == null)
                        continue;

                    string json = profile.socialLinksJson != null
                        ? System.Text.Encoding.UTF8.GetString(profile.socialLinksJson)
                        : "{}";

                    dynamic links = null;
                    try
                    {
                        links = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    }
                    catch
                    {
                        links = new { facebook = "", x = "", instagram = "" };
                    }

                    profiles.Add(new UserProfileData
                    {
                        Username = requester.nickname,
                        Email = requester.email,
                        AvatarId = profile.avatarID ?? "avatar_default",
                        NameChangeCount = requester.nameChangeCount,
                        FacebookHandle = links?.facebook ?? "",
                        XHandle = links?.x ?? "",
                        InstagramHandle = links?.instagram ?? "",
                        EmblemLayers = new List<EmblemLayer>()
                    });
                }

                return await Task.FromResult(profiles);
            }
        }
    }

    public partial class TrucoServer : ITrucoMatchService
    {
        public string CreateMatch(string hostPlayer)
        {
            throw new NotImplementedException();
        }

        public bool JoinMatch(string matchCode, string player)
        {
            throw new NotImplementedException();
        }

        public void LeaveMatch(string matchCode, string player)
        {
            throw new NotImplementedException();
        }
        public void PlayCard(string matchCode, string player, string card)
        {
            throw new NotImplementedException();
        }

        public void SendChatMessage(string matchCode, string player, string message)
        {
            throw new NotImplementedException();
        }
    }
}