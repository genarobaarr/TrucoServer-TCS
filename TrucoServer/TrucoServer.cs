using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Langs;

namespace TrucoServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public partial class TrucoServer : ITrucoUserService, ITrucoFriendService, ITrucoMatchService
    {
        private static ConcurrentDictionary<string, string> verificationCodes = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ITrucoCallback> onlineUsers = new ConcurrentDictionary<string, ITrucoCallback>();
        private const int MAX_CHANGES = 2;

        private static ITrucoCallback GetUserCallback(string username)
        {
            if (onlineUsers.TryGetValue(username, out ITrucoCallback callback))
            {
                var commObj = (ICommunicationObject)callback;
                if (commObj.State == CommunicationState.Opened)
                {
                    return callback;
                }
                commObj.Abort();
                onlineUsers.TryRemove(username, out _);
            }
            return null;
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            try
            {
                string code = new Random().Next(100000, 999999).ToString();
                verificationCodes[email] = code;
                Task.Run(() => SendVerificationEmail(email, code, languageCode));
                Console.WriteLine($"Código enviado a {email}: {code}");
                return true;
            }
            catch
            {
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
            try
            {
                LanguageManager.SetLanguage(languageCode);

                MailAddress fromAddress = new MailAddress("trucoargentinotcs@gmail.com", "Truco Argentino");
                MailAddress toAddress = new MailAddress(email);
                const string fromPassword = "obbw ipgm klkt tdxa";
                string subject = Lang.EmailVerificationSubject;
                string body = string.Format(Lang.EmailVerificationBody, code).Replace("\\n", Environment.NewLine);

                SmtpClient smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
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
            catch
            {

            }
        }
        public bool Register(string username, string password, string email)
        {
            try
            {
                using (var context = new baseDatosPruebaEntities())
                {
                    if (context.User.Any(u => u.email == email || u.nickname == username))
                    {
                        return false;
                    }

                    User user = new User
                    {
                        nickname = username,
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
            catch
            {
                return false;
            }
        }

        public bool UsernameExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            { 
                return false; 
            }
                

            using (var context = new baseDatosPruebaEntities())
            {
                return context.User.Any(u => u.nickname == username);
            }
        }

        public bool EmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            using (var context = new baseDatosPruebaEntities())
            {
                return context.User.Any(u => u.email == email);
            }
        }

        public UserProfileData GetUserProfile(string username)
        {
            using (var context = new baseDatosPruebaEntities())
            {
                User user = context.User.Include(u => u.UserProfile).FirstOrDefault(u => u.nickname == username);
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
                    Username = user.nickname,
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
                using (var context = new baseDatosPruebaEntities())
                {
                    User user = context.User.Include(u => u.UserProfile).SingleOrDefault(u => u.email == profile.Email);
                    if (user == null)
                    {
                        return false;
                    }

                    if (user.nickname != profile.Username)
                    {
                        if (user.nameChangeCount >= MAX_CHANGES)
                        {
                            return false;
                        }
                        if (context.User.Any(u => u.nickname == profile.Username && u.userID != user.userID))
                        {
                            return false;
                        }
                        user.nickname = profile.Username;
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
                using (var context = new baseDatosPruebaEntities())
                {
                    User user = context.User.FirstOrDefault(u => u.nickname == username);
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
            using (var context = new baseDatosPruebaEntities())
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
                    Username = user.nickname,
                    Email = user.email,
                    AvatarId = user.UserProfile?.avatarID ?? "avatar_aaa_default",
                    NameChangeCount = user.nameChangeCount,
                    FacebookHandle = links?.facebook ?? "",
                    XHandle = links?.x ?? "",
                    InstagramHandle = links?.instagram ?? ""
                };
            }
        }


        private void SendLoginNotificationEmail(string email, string nickname, string languageCode)
        {
            try
            {
                LanguageManager.SetLanguage(languageCode);

                MailAddress fromAddress = new MailAddress("trucoargentinotcs@gmail.com", "Truco Argentino");
                MailAddress toAddress = new MailAddress(email);
                const string fromPassword = "obbw ipgm klkt tdxa";
                string subject = Lang.EmailLoginNotificationSubject;
                string body = string.Format(Lang.EmailLoginNotificactionBody, nickname, DateTime.Now).Replace("\\n", Environment.NewLine);

                SmtpClient smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
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
            catch
            {
            }
        }

        public bool Login(string username, string password, string languageCode)
        {
            using (var context = new baseDatosPruebaEntities())
            {
                User user = context.User.FirstOrDefault(u => u.email == username || u.nickname == username);
                if (user != null && PasswordHasher.Verify(password, user.passwordHash))
                {
                    ITrucoCallback callback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
                    onlineUsers.TryAdd(user.nickname, callback);

                    Task.Run(() => SendLoginNotificationEmail(user.email, user.nickname, languageCode));

                    return true;
                }
                return false;
            }
        }

        public bool PasswordReset(string email, string code, string newPassword)
        {
            if (!ConfirmEmailVerification(email, code))
            {
                return false;
            }

            using (var context = new baseDatosPruebaEntities())
            {
                User user = context.User.FirstOrDefault(u => u.email == email);
                if (user == null)
                {
                    return false;
                }

                user.passwordHash = PasswordHasher.Hash(newPassword);
                context.SaveChanges();
                return true;
            }
        }

        public bool SendFriendRequest(string fromUser, string toUser)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                User requester = db.User.FirstOrDefault(u => u.nickname == fromUser);
                User target = db.User.FirstOrDefault(u => u.nickname == toUser);

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
            using (var db = new baseDatosPruebaEntities())
            {
                User requester = db.User.FirstOrDefault(u => u.nickname == fromUser);
                User acceptor = db.User.FirstOrDefault(u => u.nickname == toUser);
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
            using (var db = new baseDatosPruebaEntities())
            {
                User u1 = db.User.FirstOrDefault(u => u.nickname == user1);
                User u2 = db.User.FirstOrDefault(u => u.nickname == user2);
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
            using (var context = new baseDatosPruebaEntities())
            {
                var user = context.User.SingleOrDefault(u => u.nickname.ToLower() == username.ToLower());
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
                              Username = u.nickname,
                              AvatarId = u.UserProfile.avatarID
                          })
                    .ToList();

                return friendsData;
            }
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            using (var context = new baseDatosPruebaEntities())
            {
                var user = context.User.SingleOrDefault(u => u.nickname.ToLower() == username.ToLower());
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
                              Username = u.nickname,
                              AvatarId = u.UserProfile.avatarID
                          })
                    .ToList();

                return pendingRequests;
            }
        }

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
        public List<PlayerStats> GetGlobalRanking()
        {
            using (var context = new baseDatosPruebaEntities())
            {
                var topPlayers = context.User
                    .OrderByDescending(u => u.wins)
                    .Take(10)
                    .Select(u => new PlayerStats
                    {
                        PlayerName = u.nickname,
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

    }
}
