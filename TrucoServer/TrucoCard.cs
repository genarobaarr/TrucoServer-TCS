using System.Runtime.Serialization;

namespace TrucoServer
{
    [DataContract]
    public enum Suit
    {
        [EnumMember(Value = "club_")]
        Club,

        [EnumMember(Value = "cup_")]
        Cup,

        [EnumMember(Value = "sword_")]
        Sword,

        [EnumMember(Value = "gold_")]
        Gold
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
            FileName = GenerateFileName(rank, suit);
        }

        private string GenerateFileName(Rank rank, Suit suit)
        {
            string suitPrefix = GetSuitPrefix(suit);

            return $"{suitPrefix}{(int)rank}";
        }

        private string GetSuitPrefix(Suit suit)
        {
            switch (suit)
            {
                case Suit.Club:
                    return "club_";

                case Suit.Cup:
                    return "cup_";

                case Suit.Sword:
                    return "sword_";

                case Suit.Gold:
                    return "gold_";

                default:
                    return string.Empty;
            }
        }
    }
}
