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
        private const int MAX_CHANGES = 2;
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
            if (profile == null || string.IsNullOrWhiteSpace(profile.Email)) return false;

            try
            {
                using (var context = new baseDatosPruebaEntities())
                {
                    var userRecord = context.User
                        .Include(u => u.UserProfile)
                        .SingleOrDefault(u => u.email == profile.Email);

                    if (userRecord == null)
                    {
                        return false;
                    }

                    if (userRecord.nickname != profile.Username)
                    {
                        if (userRecord.nameChangeCount >= MAX_CHANGES)
                        {
                            return false;
                        }

                        bool nicknameExists = context.User
                            .Any(u => u.nickname == profile.Username && u.userID != userRecord.userID);

                        if (nicknameExists)
                        {
                            return false;
                        }

                        userRecord.nickname = profile.Username;
                        userRecord.nameChangeCount = profile.NameChangeCount;
                    }

                    if (userRecord.UserProfile == null)
                    {
                        userRecord.UserProfile = new UserProfile { userID = userRecord.userID };
                        context.UserProfile.Add(userRecord.UserProfile);
                    }

                    var userProfileRecord = userRecord.UserProfile;

                    var socialLinks = new SocialLinks
                    {
                        FacebookHandle = profile.FacebookHandle,
                        XHandle = profile.XHandle,
                        InstagramHandle = profile.InstagramHandle
                    };

                    string jsonString = JsonConvert.SerializeObject(socialLinks);
                    userProfileRecord.socialLinksJson = Encoding.UTF8.GetBytes(jsonString);

                    userProfileRecord.avatarID = profile.AvatarId;

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
            throw new NotImplementedException();
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
            var user = db.User
                         .Include(u => u.UserProfile)
                         .AsNoTracking()
                         .FirstOrDefault(u => u.nickname == nickname);

            if (user == null) return null;

            return new FriendData
            {
                Username = user.nickname,
                AvatarId = user.UserProfile?.avatarID ?? "avatar_default"
            };
        }

        private UserProfileData GetUserProfileData(string nickname, baseDatosPruebaEntities db)
        {
            var user = db.User
                         .Include(u => u.UserProfile)
                         .AsNoTracking()
                         .FirstOrDefault(u => u.nickname == nickname);

            if (user == null) return null;

            var profile = user.UserProfile;

            return new UserProfileData
            {
                //Fix: Sigo trabajando en mejorar esto:
                Username = user.nickname,
                AvatarId = profile?.avatarID ?? "avatar_default",
                SocialLinksJson = profile?.socialLinksJson != null
                    ? Encoding.UTF8.GetString(profile.socialLinksJson)
                    : null
            };
        }

        public List<FriendData> GetFriends(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var user = db.User.FirstOrDefault(u => u.nickname == username);
                if (user == null) return new List<FriendData>();

                var friends1 = db.Friendship
                    .Where(f => f.userID1 == user.userID && f.status == "Accepted")
                    .Select(f => f.User2.nickname);

                var friends2 = db.Friendship
                    .Where(f => f.userID2 == user.userID && f.status == "Accepted")
                    .Select(f => f.User.nickname);

                var allFriendsNicknames = friends1.Union(friends2).ToList();

                var friendsData = allFriendsNicknames
                    .Select(n => GetFriendData(n, db))
                    .Where(fd => fd != null)
                    .ToList();

                return friendsData;
            }
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var user = db.User.FirstOrDefault(u => u.nickname == username);
                if (user == null) return new List<FriendData>();

                var pendingRequests = db.Friendship
                    .Where(f => f.userID2 == user.userID && f.status == "Pending")
                    .Select(f => f.User.nickname)
                    .ToList();

                var requestsData = pendingRequests
                    .Select(n => GetFriendData(n, db))
                    .Where(fd => fd != null)
                    .ToList();

                return requestsData;
            }
        }

        public bool SendFriendRequest(string fromUser, string toUser)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var sender = db.User.FirstOrDefault(u => u.nickname == fromUser);
                var receiver = db.User.FirstOrDefault(u => u.nickname == toUser);

                if (sender == null || receiver == null || sender.userID == receiver.userID)
                    return false;

                var existingRel = db.Friendship
                    .FirstOrDefault(f =>
                        (f.userID1 == sender.userID && f.userID2 == receiver.userID) ||
                        (f.userID1 == receiver.userID && f.userID2 == sender.userID));

                if (existingRel != null)
                    return false;

                db.Friendship.Add(new Friendship
                {
                    userID1 = sender.userID,
                    userID2 = receiver.userID,
                    status = "Pending",
                    requestDate = DateTime.Now
                });
                db.SaveChanges();

                return true;
            }
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var requester = db.User.FirstOrDefault(u => u.nickname == fromUser);
                var acceptor = db.User.FirstOrDefault(u => u.nickname == toUser);

                if (requester == null || acceptor == null) return false;

                var request = db.Friendship.FirstOrDefault(f =>
                    f.userID1 == requester.userID &&
                    f.userID2 == acceptor.userID &&
                    f.status == "Pending");

                if (request == null) return false;

                request.status = "Accepted";
                db.SaveChanges();

                return true;
            }
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var u1 = db.User.FirstOrDefault(u => u.nickname == user1);
                var u2 = db.User.FirstOrDefault(u => u.nickname == user2);

                if (u1 == null || u2 == null) return false;

                var relationships = db.Friendship.Where(f =>
                    (f.userID1 == u1.userID && f.userID2 == u2.userID) ||
                    (f.userID1 == u2.userID && f.userID2 == u1.userID)).ToList();

                if (!relationships.Any()) return false;
                db.Friendship.RemoveRange(relationships);
                db.SaveChanges();

                return true;
            }
        }

        public async Task<List<UserProfileData>> GetAcceptedFriendsDataAsync(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.nickname == username);
                if (user == null) return new List<UserProfileData>();

                var friends1 = db.Friendship
                    .Where(f => f.userID1 == user.userID && f.status == "Accepted")
                    .Select(f => f.User2.nickname);

                var friends2 = db.Friendship
                    .Where(f => f.userID2 == user.userID && f.status == "Accepted")
                    .Select(f => f.User.nickname);

                var allFriends = await friends1.Union(friends2).ToListAsync();

                var result = allFriends
                    .Select(n => GetUserProfileData(n, db))
                    .Where(p => p != null)
                    .ToList();

                return result;
            }
        }

        public async Task<List<UserProfileData>> GetPendingFriendRequestsDataAsync(string username)
        {
            using (var db = new baseDatosPruebaEntities())
            {
                var user = await db.User.FirstOrDefaultAsync(u => u.nickname == username);
                if (user == null) return new List<UserProfileData>();

                var pending = await db.Friendship
                    .Where(f => f.userID2 == user.userID && f.status == "Pending")
                    .Select(f => f.User.nickname)
                    .ToListAsync();

                var result = pending
                    .Select(n => GetUserProfileData(n, db))
                    .Where(p => p != null)
                    .ToList();

                return result;
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