using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private List<TrucoCard> cards;
        private readonly IDeckShuffler shuffler;
        public int RemainingCards => cards.Count;

        public Deck(IDeckShuffler shuffler = null)
        {
            shuffler = shuffler ?? new DefaultDeckShuffler();
            cards = InitializeDeck();
        }

        private List<TrucoCard> InitializeDeck()
        {
            var cards = new List<TrucoCard>();
            Rank[] validRanks = {
                Rank.Uno, Rank.Dos, Rank.Tres, Rank.Cuatro, Rank.Cinco,
                Rank.Seis, Rank.Siete, Rank.Diez, Rank.Once, Rank.Doce
            };

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in validRanks)
                {
                    cards.Add(new TrucoCard(rank, suit));
                }
            }
            return cards;
        }

        public void Reset()
        {
            cards.Clear();
            cards.AddRange(InitializeDeck());
        }

        public void Shuffle()
        {
            shuffler.Shuffle(cards);
        }

        public List<TrucoCard> DealHand()
        {
            if (cards.Count < 3)
            {
                throw new InvalidOperationException("No hay suficientes cartas para repartir una mano.");
            }
            var hand = cards.Take(3).ToList();
            cards.RemoveRange(0, 3);
            return hand;
        }

        public TrucoCard DrawCard()
        {
            if (cards.Count == 0)
            {
                throw new InvalidOperationException("El mazo está vacío.");
            }
            var card = cards[0];
            cards.RemoveAt(0);
            return card;
        }
    }

    public interface IDeckShuffler
    {
        void Shuffle<T>(IList<T> list);
    }

    public class DefaultDeckShuffler : IDeckShuffler
    {
        private readonly Random _random = new Random();

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
