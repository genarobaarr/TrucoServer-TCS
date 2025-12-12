using System;

namespace TrucoServer.Data.DTOs
{
    public class AttemptInformation
    {
        public int FailedCount { get; set; } = 0;
        public DateTime BlockedUntil { get; set; } = DateTime.MinValue;
        
    }
}
