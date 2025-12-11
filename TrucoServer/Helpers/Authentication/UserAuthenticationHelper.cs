using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using TrucoServer.Langs;
using TrucoServer.Security;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Authentication
{
    public class UserAuthenticationHelper : IUserAuthenticationHelper
    {
        private const int RANDOM_BUFFER_SIZE = 4;
        private const int MIN_SECURE_CODE = 100000;
        private const int MAX_SECURE_CODE_RANGE = 900000;
        private const string FALLBACK_SECURE_CODE = "000000";

        public void ValidateBruteForceStatus(string username)
        {
            if (BruteForceProtector.IsBlocked(username))
            {
                throw FaultFactory.CreateFault("TooManyAttempts", Lang.ExceptionTextTooManyAttempts);
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return null;
            }

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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(AuthenticateUser));
                throw FaultFactory.CreateFault("ServerDBErrorLogin", Lang.ExceptionTextDBErrorLogin);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(AuthenticateUser));
                throw FaultFactory.CreateFault("ServerDBErrorLogin", Lang.ExceptionTextDBErrorLogin);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(AuthenticateUser));
                throw FaultFactory.CreateFault("ServerTimeout", Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(AuthenticateUser));
                throw FaultFactory.CreateFault("ServerError", Lang.ExceptionTextErrorOcurred);
            }
        }

        public string GenerateSecureNumericCode()
        {
            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] buffer = new byte[RANDOM_BUFFER_SIZE];

                    rng.GetBytes(buffer);
                    uint value = BitConverter.ToUInt32(buffer, 0);

                    string secureCode = (value % MAX_SECURE_CODE_RANGE + MIN_SECURE_CODE).ToString();

                    return secureCode;
                }
            }
            catch (CryptographicException ex)
            {
                ServerException.HandleException(ex, nameof(GenerateSecureNumericCode));
                return FALLBACK_SECURE_CODE;
            }
            catch (OutOfMemoryException ex)
            {
                ServerException.HandleException(ex, nameof(GenerateSecureNumericCode));
                return FALLBACK_SECURE_CODE;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GenerateSecureNumericCode));
                return FALLBACK_SECURE_CODE;
            }
        }
    }
}
