using System.Runtime.Serialization;

namespace TrucoServer
{
    [DataContract]
    public class MatchResult
    {
        [DataMember] public string Player1 { get; set; }
        [DataMember] public string Player2 { get; set; }
        [DataMember] public string Winner { get; set; }
        [DataMember] public string Date { get; set; }
    }
}
