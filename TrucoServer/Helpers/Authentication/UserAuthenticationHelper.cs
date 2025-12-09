using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using TrucoServer.Data.DTOs;
using TrucoServer.Langs;
using TrucoServer.Security;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Authentication
{
    public class UserAuthenticationHelper : IUserAuthenticationHelper
    {
        public void ValidateBruteForceStatus(string username)
        {
            if (BruteForceProtector.IsBlocked(username))
            {
                var fault = new CustomFault
                {
                    ErrorCode = "TooManyAttempts",
                    ErrorMessage = Langs.Lang.ExceptionTextTooManyAttempts
                };

                throw new FaultException<CustomFault>(fault, new FaultReason("TooManyAttempts"));
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
                    byte[] buffer = new byte[4];

                    rng.GetBytes(buffer);
                    uint value = BitConverter.ToUInt32(buffer, 0);

                    string secureCode = (value % 900000 + 100000).ToString();

                    return secureCode;
                }
            }
            catch (CryptographicException ex)
            {
                ServerException.HandleException(ex, nameof(GenerateSecureNumericCode));
                return "000000";
            }
            catch (OutOfMemoryException ex)
            {
                ServerException.HandleException(ex, nameof(GenerateSecureNumericCode));
                return "000000";
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GenerateSecureNumericCode));
                return "000000";
            }
        }
    }
}
