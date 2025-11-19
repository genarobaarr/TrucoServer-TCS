using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    [DataContract]
    public enum Suit
    {
        [EnumMember] club_,
        [EnumMember] cup_,
        [EnumMember] sword_,
        [EnumMember] gold_
    }

    [DataContract]
    public enum Rank
    {
        [EnumMember] Uno = 1,
        [EnumMember] Dos = 2,
        [EnumMember] Tres = 3,
        [EnumMember] Cuatro = 4,
        [EnumMember] Cinco = 5,
        [EnumMember] Seis = 6,
        [EnumMember] Siete = 7,
        [EnumMember] Diez = 10,
        [EnumMember] Once = 11,
        [EnumMember] Doce = 12
    }

    [DataContract]
    public class TrucoCard
    {
        [DataMember]
        public Rank CardRank { get; set; }
        
        [DataMember]
        public Suit CardSuit { get; set; }
        
        [DataMember]
        public string FileName { get; set; }

        public TrucoCard(Rank rank, Suit suit)
        {
            CardRank = rank;
            CardSuit = suit;
            FileName = $"{suit}{(int)rank}";
        }
    }
}
