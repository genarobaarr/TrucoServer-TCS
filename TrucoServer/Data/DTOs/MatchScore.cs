using System;
using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class MatchScore
    {
        [DataMember] public string MatchID { get; set; }
        [DataMember] public DateTime EndedAt { get; set; }
        [DataMember] public bool IsWin { get; set; }
        [DataMember] public int FinalScore { get; set; }
    }
}
