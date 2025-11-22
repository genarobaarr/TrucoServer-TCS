namespace TrucoServer.Utilities
{
    public class EmailSettings
    {
        public string FromAddress { get; set; }
        public string FromDisplayName { get; set; }
        public string FromPassword { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
    }
}
