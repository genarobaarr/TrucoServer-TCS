using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public enum Suit
    {
        club_,
        cup_,
        sword_,
        gold_
    }

    public enum Rank
    {
        Uno = 1, 
        Dos = 2, 
        Tres = 3, 
        Cuatro = 4, 
        Cinco = 5,
        Seis = 6, 
        Siete = 7, 
        Diez = 10, 
        Once = 11, 
        Doce = 12
    }

    public class TrucoCard
    {
        public Rank CardRank { get; private set; }
        public Suit CardSuit { get; private set; }
        public string FileName { get; private set; }

        public TrucoCard(Rank rank, Suit suit)
        {
            CardRank = rank;
            CardSuit = suit;
            FileName = $"{suit}{rank}";
        }
    }
}
