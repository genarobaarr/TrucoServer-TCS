using System;
using System.Linq;
using TrucoServer.Helpers.Email;
using TrucoServer.Utilities;
using TrucoServer.Data.DTOs;

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
            catch (Exception ex)
            {
                ServerException.HandleException(ex, context.CallingMethod);
                return false;
            }
        }
    }
}
