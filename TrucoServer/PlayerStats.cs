using System.Runtime.Serialization;

namespace TrucoServer
{
    [DataContract]
    public class PlayerStats
    {
        [DataMember] public string PlayerName { get; set; }
        [DataMember] public int Wins { get; set; }
        [DataMember] public int Losses { get; set; }
    }
}
