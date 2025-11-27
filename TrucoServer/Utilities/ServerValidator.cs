using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TrucoServer.Utilities
{
    public static class ServerValidator
    {
        private const int MIN_PASSWORD_LENGTH = 12;
        private const int MAX_PASSWORD_LENGTH = 64;
        private const int MAX_USERNAME_LENGTH = 20;

        private static readonly TimeSpan regexTimeout = TimeSpan.FromMilliseconds(150);

        private const string USERNAME_PATTERN = @"^[a-zA-Z0-9_.@-]+$";
        private const string EMAIL_PATTERN = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
        private const string PASSWORD_PATTERN = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_+=-])[A-Za-z\d@$!%*?&.#_+=-]{8,64}$";
        private const string MATCH_CODE_PATTERN = @"^[A-Z0-9]{6}$";

        public static bool IsUsernameValid(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            username = username.Normalize(NormalizationForm.FormC);

            if (username.Length > MAX_USERNAME_LENGTH)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(username, USERNAME_PATTERN, RegexOptions.None, regexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsEmailValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            email = email.Normalize(NormalizationForm.FormC);

            try
            {
                return Regex.IsMatch(email, EMAIL_PATTERN, RegexOptions.None, regexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsPasswordValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            password = password.Normalize(NormalizationForm.FormC);

            if (password.Length < MIN_PASSWORD_LENGTH || password.Length > MAX_PASSWORD_LENGTH)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(password, PASSWORD_PATTERN, RegexOptions.None, regexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsMatchCodeValid(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            code = code.Normalize(NormalizationForm.FormC);

            try
            {
                return Regex.IsMatch(code, MATCH_CODE_PATTERN, RegexOptions.None, regexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsIdValid(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            id = id.Normalize(NormalizationForm.FormC);

            return int.TryParse(id, out int parsed) && parsed > 0;
        }
    }
}
