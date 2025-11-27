using System;
using System.Collections.Concurrent;
using System.Net.Mail;
using System.Threading.Tasks;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Email;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Verification
{
    public class VerificationService : IVerificationService
    {
        private static readonly ConcurrentDictionary<string, string> verificationCodes = new ConcurrentDictionary<string, string>();
        private readonly IUserAuthenticationHelper authenticationHelper;
        private readonly IEmailSender emailSender;

        public VerificationService(IUserAuthenticationHelper authenticationHelper, IEmailSender emailSender)
        {
            this.authenticationHelper = authenticationHelper;
            this.emailSender = emailSender;
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            if (!ServerValidator.IsEmailValid(email))
            {
                return false;
            }

            try
            {
                string code = authenticationHelper.GenerateSecureNumericCode();
                verificationCodes[email] = code;

                Langs.LanguageManager.SetLanguage(languageCode);

                Task.Run(() => emailSender.SendEmail(email, Langs.Lang.EmailVerificationSubject,
                    string.Format(Langs.Lang.EmailVerificationBody, code).Replace("\\n", Environment.NewLine)));

                Console.WriteLine($"[EMAIL] Code Sended To {email}: {code}");
                return true;
            }
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, $"{nameof(RequestEmailVerification)} - Argument Null");
            }
            catch (SmtpException ex)
            {
                LogManager.LogError(ex, $"{nameof(RequestEmailVerification)} - SMTP Error");
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, $"{nameof(RequestEmailVerification)} - Invalid Operation");
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(RequestEmailVerification));
            }
            return false;
        }

        public bool ConfirmEmailVerification(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            if (verificationCodes.TryGetValue(email, out string storedCode) && storedCode == code)
            {
                verificationCodes.TryRemove(email, out _);
                return true;
            }

            return false;
        }
    }
}
