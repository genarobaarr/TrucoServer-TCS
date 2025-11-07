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

        /*
        * Podríamos mostrar si el amigo está online
        * [DataMember]
        * public bool IsOnline { get; set; }
        */
    }
}
