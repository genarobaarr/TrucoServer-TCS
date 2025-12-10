using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
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

        public static bool RegisterOffense(string username)
        {
            int currentCount = offenses.AddOrUpdate(username, 1, (key, oldValue) => oldValue + 1);
            return currentCount >= MAX_OFFENSES;
        }

        public static void ResetOffenses(string username)
        {
            offenses.TryRemove(username, out _);
        }

        public void BanUser(string username, string reason)
        {
            if (string.IsNullOrEmpty(username))
            {
                return;
            }

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
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(BanUser));
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(BanUser));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(BanUser));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(BanUser));
            }
        }

        public void ValidateBanStatus(string username)
        {
            if (IsUserBanned(username))
            {
                var fault = new CustomFault
                {
                    ErrorCode = "UserBanned",
                    ErrorMessage = Langs.Lang.ExceptionTextUserBanned
                };
                throw new FaultException<CustomFault>(fault, new FaultReason("UserBanned"));
            }
        }

        public bool IsUserBanned(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

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
            catch (SqlException ex)
            { 
                ServerException.HandleException(ex, nameof(IsUserBanned));
                return false;
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(IsUserBanned));
                return false;
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(IsUserBanned));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(IsUserBanned));
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(IsUserBanned));
                return false;
            }
        }
    }
}
