using System;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;
using TrucoServer.Security;

namespace TrucoServer.Helpers.Authentication
{
    public interface IUserAuthenticationHelper
    {
        void ValidateBruteForceStatus(string username);
        User AuthenticateUser(string username, string password);
        string GenerateSecureNumericCode();
    }
}
