using System.Runtime.Serialization;

namespace TrucoServer
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
