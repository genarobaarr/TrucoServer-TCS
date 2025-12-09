using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Email;
using TrucoServer.Langs;
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

                LanguageManager.SetLanguage(languageCode);

                var emailOptions = new EmailFormatOptions
                {
                    ToEmail = email,
                    EmailSubject = Lang.EmailVerificationSubject,
                    EmailBody = string.Format(Lang.EmailVerificationBody, code)
                                       .Replace("\\n", Environment.NewLine)
                };

                emailSender.SendEmail(emailOptions);

                return true;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerSmtpError", Lang.ExceptionTextSmtpVerification);
            }
            catch (SmtpException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerSmtpError", Lang.ExceptionTextSmtpVerification);
            }
            catch (WebException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerSmtpError", Lang.ExceptionTextSmtpVerification);
            }
            catch (CultureNotFoundException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
            catch (FormatException ex)
            { 
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(RequestEmailVerification));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
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
