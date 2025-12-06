using System.Runtime.Serialization;

namespace TrucoServer.Data.DTOs
{
    public class EmailFormatOptions
    {
        public string ToEmail { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
    }
}
