using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class InviteFriendOptions
    {
        [DataMember] public string MatchCode { get; set; }
        [DataMember] public string SenderUsername { get; set; }
        [DataMember] public string FriendUsername { get; set; }
    }
}
