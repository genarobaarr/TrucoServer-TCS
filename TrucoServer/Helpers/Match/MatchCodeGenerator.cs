using System;
using TrucoServer.Utilities;
using System.Runtime.Remoting;
using System.Security.Cryptography;

namespace TrucoServer.Helpers.Match
{
    public class MatchCodeGenerator : IMatchCodeGenerator
    {
        private const int MATCH_CODE_LENGTH = 6;
        private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public string GenerateMatchCode()
        {
            char[] result = new char[MATCH_CODE_LENGTH];

            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] randomBytes = new byte[result.Length];
                    rng.GetBytes(randomBytes);

                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = CHARS[randomBytes[i] % CHARS.Length];
                    }
                }

                return new string(result);
            }
            catch (CryptographicException ex)
            {
                Utilities.ServerException.HandleException(ex, nameof(GenerateMatchCode));
                return string.Empty;
            }
            catch (DivideByZeroException ex)
            {
                Utilities.ServerException.HandleException(ex, nameof(GenerateMatchCode));
                return string.Empty;
            }
            catch (IndexOutOfRangeException ex)
            {
                Utilities.ServerException.HandleException(ex, nameof(GenerateMatchCode));
                return string.Empty;
            }
            catch (Exception ex)
            {
                Utilities.ServerException.HandleException(ex, nameof(GenerateMatchCode));
                return string.Empty;
            }
        }

        public int GenerateNumericCodeFromString(string code)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in code)
                {
                    hash = hash * 31 + c;
                }
                return Math.Abs(hash % 999999);
            }
        }
    }
}
