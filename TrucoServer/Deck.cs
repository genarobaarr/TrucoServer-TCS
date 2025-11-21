using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace TrucoServer
{
    public interface ITrucoDeck
    {
        void Shuffle();
        List<TrucoCard> DealHand();
        TrucoCard DrawCard();
        void Reset();
        int RemainingCards { get; }
    }

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
            catch (OutOfMemoryException ex)
            {
                LogManager.LogError(ex, nameof(Reset));
                throw; 
            }
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, nameof(Reset));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(Reset));
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
                LogManager.LogError(ex, nameof(Shuffle));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(Shuffle));
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
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(DealHand));
                throw; 
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.LogError(ex, nameof(DealHand));
                return new List<TrucoCard>();
            }
            catch (ArgumentNullException ex) 
            {
                LogManager.LogError(ex, nameof(DealHand));
                return new List<TrucoCard>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(DealHand));
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
            catch (InvalidOperationException ex)
            {
                LogManager.LogWarn(ex.Message, nameof(DrawCard));
                throw;
            }
            catch (ArgumentOutOfRangeException ex) 
            {
                LogManager.LogError(ex, nameof(DrawCard));
                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(DrawCard));
                return null;
            }
        }
    }

    public interface IDeckShuffler
    {
        void Shuffle<T>(IList<T> list);
    }

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
            catch (ArgumentNullException ex)
            {
                LogManager.LogError(ex, nameof(Shuffle));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(Shuffle));
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
