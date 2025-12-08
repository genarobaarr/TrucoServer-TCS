using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Email;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Password
{
    public class PasswordManager : IPasswordManager
    {
        private readonly IEmailSender emailSender;

        public PasswordManager(IEmailSender emailSender)
        {
            this.emailSender = emailSender;
        }

        public bool UpdatePasswordAndNotify(PasswordUpdateOptions context)
        {
            if (context == null)
            {
                return false;
            }

            try
            {
                using (var dbContext = new baseDatosTrucoEntities())
                {
                    User user = dbContext.User.FirstOrDefault(u => u.email == context.Email);

                    if (user == null)
                    {
                        return false;
                    }

                    user.passwordHash = PasswordHasher.Hash(context.NewPassword);

                    dbContext.SaveChanges();

                    Langs.LanguageManager.SetLanguage(context.LanguageCode);

                    emailSender.NotifyPasswordChange(user);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, context.CallingMethod);
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, context.CallingMethod);
                return false;
            }
            catch (SmtpException ex)
            {
                ServerException.HandleException(ex, context.CallingMethod);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, context.CallingMethod);
                return false;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, context.CallingMethod);
                return false;
            }
        }
    }
}
