using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class LoginFault
    {
        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string ErrorCode { get; set; }
    }
}