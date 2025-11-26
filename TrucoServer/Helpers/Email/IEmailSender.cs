namespace TrucoServer.Helpers.Email
{
    public interface IEmailSender
    {
        void SendEmail(string toEmail, string emailSubject, string emailBody);
        void SendLoginEmailAsync(User user);
        void NotifyPasswordChange(User user);
    }
}
