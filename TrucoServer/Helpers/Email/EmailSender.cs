using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Email
{
    public class EmailSender : IEmailSender
    {
        public void SendEmail(string toEmail, string emailSubject, string emailBody)
        {
            try
            {
                var settings = ConfigurationReader.EmailSettings;
                var fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
                var toAddress = new MailAddress(toEmail);

                using (var smtp = new SmtpClient
                {
                    Host = settings.SmtpHost,
                    Port = settings.SmtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, settings.FromPassword)
                })
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = emailSubject,
                    Body = emailBody
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SendEmail));
            }
        }

        public void SendLoginEmailAsync(User user)
        {
            Task.Run(() =>
            {
                try
                {
                    SendEmail(user.email, Langs.Lang.EmailLoginNotificationSubject,
                        string.Format(Langs.Lang.EmailLoginNotificactionBody, user.username, DateTime.Now)
                        .Replace("\\n", Environment.NewLine));
                }
                catch (Exception ex)
                {
                    ServerException.HandleException(ex, nameof(SendLoginEmailAsync));
                }
            });
        }

        public void NotifyPasswordChange(User user)
        {
            Task.Run(() => SendEmail(user.email, Langs.Lang.EmailPasswordNotificationSubject,
                string.Format(Langs.Lang.EmailPasswordNotificationBody, user.username, DateTime.Now).Replace("\\n", Environment.NewLine)));
        }
    }
}
