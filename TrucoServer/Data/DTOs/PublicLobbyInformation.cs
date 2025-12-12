using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class PublicLobbyInformation
    {
        [DataMember] public string MatchName { get; set; }

        [DataMember] public string MatchCode { get; set; }

        [DataMember] public int CurrentPlayers { get; set; }

        [DataMember] public int MaxPlayers { get; set; }
    }
}

