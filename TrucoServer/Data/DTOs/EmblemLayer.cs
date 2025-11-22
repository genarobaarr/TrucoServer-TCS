using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
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
}
