using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class FriendData
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string AvatarId { get; set; }

    }
}
