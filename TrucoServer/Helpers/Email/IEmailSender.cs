using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Email
{
    public interface IEmailSender
    {
        void SendEmail(EmailFormatOptions emailOptions);
        void SendLoginEmailAsync(User user);
        void SendInvitationEmailAsync(InviteFriendData friendEmailData);
        void NotifyPasswordChange(User user);
    }
}
