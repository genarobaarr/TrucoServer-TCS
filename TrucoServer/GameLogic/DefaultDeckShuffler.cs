using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public class DefaultDeckShuffler : IDeckShuffler
    {
        private const string TEXT_ARGUMENT_EXCEPTION_LIST_NULL = "The provided list cannot be null.";

        public void Shuffle<T>(IList<T> list)
        {
            try
            {
                if (list == null)
                {
                    throw new ArgumentNullException(nameof(list), TEXT_ARGUMENT_EXCEPTION_LIST_NULL);
                }

                using (var rng = new RNGCryptoServiceProvider())
                {
                    for (int i = list.Count - 1; i > 0; i--)
                    {
                        int j = GetSecureRandomInt(rng, i + 1);

                        (list[i], list[j]) = (list[j], list[i]);
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(Shuffle));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ServerException.HandleException(ex, nameof(Shuffle));
            }
            catch (CryptographicException ex)
            {
                ServerException.HandleException(ex, nameof(Shuffle));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(Shuffle));
            }
        }

        private static int GetSecureRandomInt(RNGCryptoServiceProvider rng, int max)
        {
            byte[] buffer = new byte[4];
            rng.GetBytes(buffer);

            int result = BitConverter.ToInt32(buffer, 0);

            return Math.Abs(result) % max;
        }
    }
}
