using System;

namespace TrucoServer.Data.DTOs
{
    public class AttemptInfo
    {
        public int FailedCount { get; set; } = 0;
        public DateTime BlockedUntil { get; set; } = DateTime.MinValue;
        
    }
}
