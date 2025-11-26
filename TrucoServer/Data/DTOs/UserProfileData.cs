using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class UserProfileData
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember] 
        public string AvatarId { get; set; }
        [DataMember]
        public int NameChangeCount { get; set; }
        [DataMember]
        public string FacebookHandle { get; set; }
        [DataMember]
        public string XHandle { get; set; }
        [DataMember]
        public string InstagramHandle { get; set; }
        [DataMember]
        public List<EmblemLayer> EmblemLayers { get; set; }
        [DataMember]
        public byte[] SocialLinksJson { get; set; }
        [DataMember]
        public string LanguageCode { get; set; }
        [DataMember]
        public bool IsMusicMuted { get; set; }
    }
}