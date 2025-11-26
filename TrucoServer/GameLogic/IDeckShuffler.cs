using System.Collections.Generic;

namespace TrucoServer.GameLogic
{
    public interface IDeckShuffler
    {
        void Shuffle<T>(IList<T> list);
    }
}
