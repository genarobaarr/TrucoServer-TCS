using System.Collections.Generic;
using TrucoServer.GameLogic;

namespace TrucoServer.Contracts
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
