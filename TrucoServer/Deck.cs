using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public class Deck
    {
        private List<TrucoCard> cards;
        public Deck()
        {
            InitializeDeck();
        }
        private void InitializeDeck()
        {
            cards = new List<TrucoCard>();

            Rank[] validRanks = { Rank.Uno, Rank.Dos, Rank.Tres, Rank.Cuatro, Rank.Cinco,
                                  Rank.Seis, Rank.Siete, Rank.Diez, Rank.Once, Rank.Doce };

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in validRanks)
                {
                    cards.Add(new TrucoCard(rank, suit));
                }
            }
        }

        public void Shuffle()
        {
            Random rng = new Random();
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                TrucoCard value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
        }

        public List<TrucoCard> DealHand()
        {
            if (cards.Count < 3) return new List<TrucoCard>();

            List<TrucoCard> hand = cards.Take(3).ToList();
            cards.RemoveRange(0, 3);
            return hand;
        }
    }
}
