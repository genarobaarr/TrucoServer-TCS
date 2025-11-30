using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public class Deck : ITrucoDeck
    {
        private readonly List<TrucoCard> cards;
        private readonly IDeckShuffler shuffler;
        public int RemainingCards => cards.Count;

        public Deck(IDeckShuffler shuffler = null)
        {
            this.shuffler = shuffler ?? new DefaultDeckShuffler();
            cards = InitializeDeck();
        }

        private static List<TrucoCard> InitializeDeck()
        {
            var newCards = new List<TrucoCard>();
            Rank[] validRanks = {
                Rank.Uno, Rank.Dos, Rank.Tres, Rank.Cuatro, Rank.Cinco,
                Rank.Seis, Rank.Siete, Rank.Diez, Rank.Once, Rank.Doce
            };

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in validRanks)
                {
                    newCards.Add(new TrucoCard(rank, suit));
                }
            }
            
            return newCards;
        }

        public void Reset()
        {
            try
            {
                cards.Clear();
                cards.AddRange(InitializeDeck());
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(Reset));
            }
        }

        public void Shuffle()
        {
            try
            {
                shuffler.Shuffle(cards);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(Shuffle));
            }
        }

        public List<TrucoCard> DealHand()
        {
            try
            {
                if (cards.Count < 3)
                {
                    throw new InvalidOperationException("There are not enough cards to deal a hand.");
                }
                
                var hand = cards.Take(3).ToList();
                cards.RemoveRange(0, 3);
                
                return hand;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(DealHand));
                return new List<TrucoCard>();
            }
        }

        public TrucoCard DrawCard()
        {
            try
            {
                if (cards.Count == 0)
                {
                    throw new InvalidOperationException("The deck is empty.");
                }

                var card = cards[0];
                cards.RemoveAt(0);
                
                return card;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(DrawCard));
                return null;
            }
        }
    }
}
