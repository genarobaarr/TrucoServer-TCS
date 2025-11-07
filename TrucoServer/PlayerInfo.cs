using System.Runtime.Serialization;

namespace TrucoServer
{
    [DataContract]
    public class PlayerInfo
    {
        [DataMember] public string Username { get; set; }
        [DataMember] public string AvatarId { get; set; }
        [DataMember] public string OwnerUsername { get; set; }
    }
}
