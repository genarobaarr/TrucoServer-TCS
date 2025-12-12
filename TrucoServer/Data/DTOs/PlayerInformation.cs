using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class PlayerInformation
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public string AvatarId { get; set; }
        [DataMember] public string OwnerUsername { get; set; }
        [DataMember] public string Team { get; set; }
    }
}
