using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
{
    public class Deck : ITrucoDeck
    {
        private const int HAND_SIZE = 3;
        private const int FIRST_CARD_INDEX = 0;

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
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(Reset));
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
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(Shuffle));
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
                if (cards.Count < HAND_SIZE)
                {
                    throw new InvalidOperationException("There are not enough cards to deal a hand.");
                }
                
                var hand = cards.Take(HAND_SIZE).ToList();
                cards.RemoveRange(FIRST_CARD_INDEX, HAND_SIZE);
                
                return hand;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(DealHand));
               
                return new List<TrucoCard>();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ServerException.HandleException(ex, nameof(DealHand));
                
                return new List<TrucoCard>();
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

                var card = cards[FIRST_CARD_INDEX];
                cards.RemoveAt(FIRST_CARD_INDEX);
                
                return card;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(DrawCard));
                
                return null;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ServerException.HandleException(ex, nameof(DrawCard));
                
                return null;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(DrawCard));
                
                return null;
            }
        }
    }
}
