using System.Collections.Generic;

namespace TrucoServer.GameLogic
{
    public interface ITrucoDeck
    {
        void Shuffle();
        List<TrucoCard> DealHand();
        TrucoCard DrawCard();
        void Reset();
        int RemainingCards { get; }
    }
}
