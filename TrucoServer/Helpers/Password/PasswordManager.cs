using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
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

        public bool UpdatePasswordAndNotify(string email, string newPassword, string languageCode, string callingMethod)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    User user = context.User.FirstOrDefault(u => u.email == email);

                    if (user == null)
                    {
                        return false;
                    }

                    user.passwordHash = PasswordHasher.Hash(newPassword);
                    context.SaveChanges();

                    Langs.LanguageManager.SetLanguage(languageCode);
                    emailSender.NotifyPasswordChange(user);

                    return true;
                }
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, $"{callingMethod} - DataBase Saving Error");
                return false;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{callingMethod} - SQL Server Error");
                return false;
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, $"{callingMethod} - Email Error");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, callingMethod);
                return false;
            }
        }
    }
}
