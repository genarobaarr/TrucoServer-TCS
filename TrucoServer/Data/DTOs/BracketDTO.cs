using System;
using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class BracketDTO
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public int Round { get; set; }
        [DataMember] public int Position { get; set; }
        [DataMember] public string Player1Name { get; set; }
        [DataMember] public string Player2Name { get; set; }
        [DataMember] public string WinnerName { get; set; }
        [DataMember] public string MatchId { get; set; }
    }
}
