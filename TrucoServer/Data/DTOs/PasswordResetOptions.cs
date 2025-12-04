using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    [DataContract]
    public class PasswordResetOptions
    {
        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string NewPassword { get; set; }

        [DataMember]
        public string LanguageCode { get; set; }
    }
}