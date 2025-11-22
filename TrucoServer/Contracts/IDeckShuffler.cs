using System.Collections.Generic;

namespace TrucoServer.Contracts
{
    public interface IDeckShuffler
    {
        void Shuffle<T>(IList<T> list);
    }
}
