using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;
using TrucoServer.Langs;

namespace TrucoServer.Helpers.Email
{
    public class EmailSender : IEmailSender
    {
        public void SendEmail(EmailFormatOptions emailOptions)
        {
            try
            {
                if (emailOptions == null)
                {
                    throw new ArgumentNullException(nameof(emailOptions));
                }

                var settings = ConfigurationReader.EmailSettings;

                var fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
                var toAddress = new MailAddress(emailOptions.ToEmail);

                using (var smtp = new SmtpClient
                {
                    Host = settings.SmtpHost,
                    Port = settings.SmtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, settings.FromPassword),
                })
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = emailOptions.EmailSubject,
                    Body = emailOptions.EmailBody
                })
                {
                    smtp.Send(message);
                }
            }
            catch (SmtpFailedRecipientsException ex)
            {
                ServerException.HandleException(ex, nameof(SendEmail));
            }
            catch (SmtpException ex)
            {
                ServerException.HandleException(ex, nameof(SendEmail));
            }
            catch (FormatException ex)
            {
                ServerException.HandleException(ex, nameof(SendEmail));
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(SendEmail));
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
                    if (user == null)
                    {
                        throw new InvalidOperationException("User cannot be null");
                    }

                    var emailOptions = new EmailFormatOptions
                    {
                        ToEmail = user.email,
                        EmailSubject = Lang.EmailLoginNotificationSubject,
                        EmailBody = string.Format(Lang.EmailLoginNotificactionBody, user.username, DateTime.Now)
                            .Replace("\\n", Environment.NewLine)
                    };

                    SendEmail(emailOptions);
                }
                catch (InvalidOperationException ex)
                {
                    ServerException.HandleException(ex, nameof(SendLoginEmailAsync));
                }
                catch (FormatException ex)
                {
                    ServerException.HandleException(ex, nameof(SendLoginEmailAsync));
                }
                catch (Exception ex)
                {
                    ServerException.HandleException(ex, nameof(SendLoginEmailAsync));
                }
            });
        }

        public void SendInvitationEmailAsync(InviteFriendData friendEmailData)
        {
            Task.Run(() =>
            {
                try
                {
                    if (friendEmailData?.FriendUser == null || friendEmailData?.SenderUser == null)
                    {
                        throw new InvalidOperationException("Friend email data cannot be null");
                    }

                    var emailOptions = new EmailFormatOptions
                    {
                        ToEmail = friendEmailData.FriendUser.email,
                        EmailSubject = Lang.EmailInvitationSubject,
                        EmailBody = string.Format(Lang.EmailInvitationBody, friendEmailData.FriendUser.username,
                            friendEmailData.SenderUser.username, friendEmailData.MatchCode)
                            .Replace("\\n", Environment.NewLine)
                    };

                    SendEmail(emailOptions);
                }
                catch (InvalidOperationException ex)
                {
                    ServerException.HandleException(ex, nameof(SendInvitationEmailAsync));
                }
                catch (FormatException ex)
                {
                    ServerException.HandleException(ex, nameof(SendInvitationEmailAsync));
                }
                catch (Exception ex)
                {
                    ServerException.HandleException(ex, nameof(SendInvitationEmailAsync));
                }
            });
        }

        public void NotifyPasswordChange(User user)
        {
            var emailOptions = new EmailFormatOptions
            {
                ToEmail = user.email,
                EmailSubject = Lang.EmailPasswordNotificationSubject,
                EmailBody = string.Format(Lang.EmailPasswordNotificationBody, user.username, DateTime.Now)
                    .Replace("\\n", Environment.NewLine)
            };

            Task.Run(() => SendEmail(emailOptions));
        }
    }
}
