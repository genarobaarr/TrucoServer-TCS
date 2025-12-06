using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Security
{
    public class BanService
    {
        private const int MAX_OFFENSES = 5;
        private const int BAN_DURATION_MINUTES = 5;

        private static readonly ConcurrentDictionary<string, int> offenses
            = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly baseDatosTrucoEntities context;

        public BanService(baseDatosTrucoEntities context)
        {
            this.context = context;
        }

        public bool RegisterOffense(string username)
        {
            int currentCount = offenses.AddOrUpdate(username, 1, (key, oldValue) => oldValue + 1);
            return currentCount >= MAX_OFFENSES;
        }

        public void ResetOffenses(string username)
        {
            offenses.TryRemove(username, out _);
        }

        public void BanUser(string username, string reason)
        {
            try
            {
                var user = context.User.FirstOrDefault(u => u.username == username);
                if (user == null)
                {
                    return;
                }

                var ban = new Ban
                {
                    userID = user.userID,
                    reason = reason,
                    expiresAt = DateTime.Now.AddMinutes(BAN_DURATION_MINUTES)
                };

                context.Ban.Add(ban);
                context.SaveChanges();

                ResetOffenses(username);
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(BanUser));
            }
        }

        public void ValidateBanStatus(string username)
        {
            if (IsUserBanned(username))
            {
                var fault = new LoginFault
                {
                    ErrorCode = "UserBanned",
                    ErrorMessage = Langs.Lang.ExceptionTextUserBanned
                };
                throw new FaultException<LoginFault>(fault, new FaultReason("UserBanned"));
            }
        }

        public bool IsUserBanned(string username)
        {
            try
            {
                var user = context.User.FirstOrDefault(u => u.username == username);
                if (user == null)
                {
                    return false;
                }

                bool isBanned = context.Ban.Any(b => b.userID == user.userID && b.expiresAt > DateTime.Now);
                return isBanned;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(IsUserBanned));
                return false;
            }
        }
    }
}
