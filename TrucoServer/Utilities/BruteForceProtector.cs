using System;
using System.Collections.Concurrent;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Security
{
    public static class BruteForceProtector
    {
        private const int MAX_ATTEMPTS = 5;
        private const int BAN_MINUTES = 5;
        private static readonly ConcurrentDictionary<string, AttemptInfo> attempts
            = new ConcurrentDictionary<string, AttemptInfo>();

        public static bool IsBlocked(string identifier)
        {
            if (attempts.TryGetValue(identifier, out AttemptInfo info) && info.BlockedUntil > DateTime.UtcNow)
            {
                return true;
            }

            return false;
        }

        public static void RegisterFailedAttempt(string identifier)
        {
            var record = attempts.GetOrAdd(identifier, _ => new AttemptInfo());
            record.FailedCount++;

            if (record.FailedCount >= MAX_ATTEMPTS)
            {
                record.BlockedUntil = DateTime.UtcNow.AddMinutes(BAN_MINUTES);
                record.FailedCount = 0;
            }
        }

        public static void RegisterSuccess(string identifier)
        {
            attempts.TryRemove(identifier, out _);
        }
    }
}
