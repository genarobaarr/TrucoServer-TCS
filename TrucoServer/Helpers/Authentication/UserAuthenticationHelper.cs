using System;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using TrucoServer.Security;
using TrucoServer.Utilities;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Authentication
{
    public class UserAuthenticationHelper : IUserAuthenticationHelper
    {
        public void ValidateBruteForceStatus(string username)
        {
            if (BruteForceProtector.IsBlocked(username))
            {
                var fault = new LoginFault
                {
                    ErrorCode = "TooManyAttempts",
                    ErrorMessage = Langs.Lang.ExceptionTextTooManyAttempts
                };

                throw new FaultException<LoginFault>(fault, new FaultReason("TooManyAttempts"));
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.email == username || u.username == username);

                    if (user == null || !PasswordHasher.Verify(password, user.passwordHash))
                    {
                        BruteForceProtector.RegisterFailedAttempt(username);
                        return null;
                    }

                    return user;
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                LogManager.LogError(ex, "Login - Database Error");
                return null;
            }
        }

        public string GenerateSecureNumericCode()
        {
            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] buffer = new byte[4];

                    rng.GetBytes(buffer);
                    uint value = BitConverter.ToUInt32(buffer, 0);

                    string secureCode = (value % 900000 + 100000).ToString();

                    return secureCode;
                }
            }
            catch (CryptographicException ex)
            {
                LogManager.LogError(ex, $"{nameof(GenerateSecureNumericCode)} - Cryptographic Provider Error");
                return "000000";
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GenerateSecureNumericCode));
                return "000000";
            }
        }
    }
}
