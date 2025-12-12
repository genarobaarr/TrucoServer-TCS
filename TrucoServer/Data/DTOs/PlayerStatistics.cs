using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class PlayerStatistics
    {
        [DataMember] public string PlayerName { get; set; }
        [DataMember] public int Wins { get; set; }
        [DataMember] public int Losses { get; set; }
    }
}
