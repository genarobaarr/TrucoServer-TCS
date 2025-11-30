using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public class DefaultDeckShuffler : IDeckShuffler
    {
        public void Shuffle<T>(IList<T> list)
        {
            try
            {
                if (list == null)
                {
                    throw new ArgumentNullException(nameof(list), "The list to be shuffled cannot be null.");
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
