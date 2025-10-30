using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
