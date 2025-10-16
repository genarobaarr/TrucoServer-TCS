using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TrucoServer
{
    [DataContract]
    public class EmblemLayer
    {
        [DataMember]
        public int ShapeId { get; set; }
        [DataMember]
        public string ColorHex { get; set; }
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double ScaleX { get; set; }
        [DataMember]
        public double ScaleY { get; set; }
        [DataMember]
        public double Rotation { get; set; }
        [DataMember]
        public int ZIndex { get; set; }
    }

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
    }
}