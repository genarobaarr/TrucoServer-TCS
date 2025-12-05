namespace TrucoServer.Data.DTOs
{
    public class PasswordUpdateOptions
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string LanguageCode { get; set; }
        public string CallingMethod { get; set; }
    }
}
