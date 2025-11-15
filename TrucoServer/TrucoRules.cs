using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public static class TrucoRules
    {
        private const int ONE = 1;
        private const int TWO = 2;
        private const int THREE = 3;
        private const int FOUR = 4; 
        private const int FIVE = 5;
        private const int SIX = 6;
        private const int SEVEN = 7;
        private const int EIGHT = 8;
        private const int NINE = 9;
        private const int TEN = 10;
        private const int ELEVEN = 11;
        private const int TWELVE = 12;
        private const int THIRTEEN = 13;
        private const int FOURTEEN = 14;

        private static readonly Dictionary<(Rank, Suit), int> trucoValueMap = new Dictionary<(Rank, Suit), int>
        {
            
            { 
                (Rank.Uno, Suit.sword_), FOURTEEN 
            }, 
            { 
                (Rank.Uno, Suit.club_), THIRTEEN
            },  
            { 
                (Rank.Siete, Suit.sword_), TWELVE
            },
            { 
                (Rank.Siete, Suit.gold_), ELEVEN
            },                
            
            { 
                (Rank.Tres, Suit.sword_), TEN 
            }, 
            { 
                (Rank.Tres, Suit.club_), TEN
            },
            { 
                (Rank.Tres, Suit.cup_), TEN
            }, 
            { 
                (Rank.Tres, Suit.gold_), TEN
            },
            
            { 
                (Rank.Dos, Suit.sword_), NINE
            }, 
            { 
                (Rank.Dos, Suit.club_), NINE
            },
            { 
                (Rank.Dos, Suit.cup_), NINE
            }, 
            { 
                (Rank.Dos, Suit.gold_), NINE
            }, 
            
            { 
                (Rank.Uno, Suit.cup_), EIGHT
            }, 
            { 
                (Rank.Uno, Suit.gold_), EIGHT
            }, 
            { 
                (Rank.Doce, Suit.sword_), SEVEN
            }, 
            { 
                (Rank.Doce, Suit.club_), SEVEN
            },
            { 
                (Rank.Doce, Suit.cup_), SEVEN
            }, 
            { 
                (Rank.Doce, Suit.gold_), SEVEN
            }, 
            { 
                (Rank.Once, Suit.sword_), SIX
            }, 
            { 
                (Rank.Once, Suit.club_), SIX
            },
            { 
                (Rank.Once, Suit.cup_), SIX
            }, 
            { 
                (Rank.Once, Suit.gold_), SIX
            },
            { 
                (Rank.Diez, Suit.sword_), FIVE
            }, 
            { 
                (Rank.Diez, Suit.club_), FIVE
            },
            { 
                (Rank.Diez, Suit.cup_), FIVE
            }, 
            { 
                (Rank.Diez, Suit.gold_), FIVE
            }, 
            
            { 
                (Rank.Siete, Suit.club_), FOUR
            }, 
            { 
                (Rank.Siete, Suit.cup_), FOUR
            }, 
            
            { 
                (Rank.Seis, Suit.sword_), THREE 
            }, 
            { 
                (Rank.Seis, Suit.club_), THREE
            },
            { 
                (Rank.Seis, Suit.cup_), THREE
            }, 
            { 
                (Rank.Seis, Suit.gold_), THREE
            }, 
            
            { 
                (Rank.Cinco, Suit.sword_), TWO
            }, 
            { 
                (Rank.Cinco, Suit.club_), TWO 
            },
            { 
                (Rank.Cinco, Suit.cup_), TWO
            }, 
            { 
                (Rank.Cinco, Suit.gold_), TWO 
            }, 
            { 
                (Rank.Cuatro, Suit.sword_), ONE
            }, 
            { 
                (Rank.Cuatro, Suit.club_), ONE
            },
            { 
                (Rank.Cuatro, Suit.cup_), ONE
            }, 
            { 
                (Rank.Cuatro, Suit.gold_), ONE
            }
        };

        public static int GetTrucoValue(TrucoCard card)
        {
            if (trucoValueMap.TryGetValue((card.CardRank, card.CardSuit), out int value))
            {
                return value;
            }
            LogManager.LogError(new InvalidOperationException($"Carta no mapeada: {card.FileName}"), nameof(GetTrucoValue));
            return 0;
        }

        public static int CompareCards(TrucoCard cardA, TrucoCard cardB)
        {
            int valueA = GetTrucoValue(cardA);
            int valueB = GetTrucoValue(cardB);

            if (valueA > valueB) return 1; 
            if (valueB > valueA) return -1;
            return 0;
        }

        public static int CalculateEnvidoScore(List<TrucoCard> hand)
        {
            var groups = hand.GroupBy(card => card.CardSuit);
            var bestGroup = groups.OrderByDescending(g => g.Count()).FirstOrDefault();
            if (bestGroup == null || bestGroup.Count() < 2)
            {
                return hand.Max(card => GetEnvidoValue(card));
            }
            else
            {
                var twoHighest = bestGroup.OrderByDescending(card => GetEnvidoValue(card)).Take(2).ToList();
                return GetEnvidoValue(twoHighest[0]) + GetEnvidoValue(twoHighest[1]) + 20;
            }
        }

        // Aquí iría la lógica para calcular el Envido/Flor/Truco
        public static int GetEnvidoValue(TrucoCard card)
        {
            if ((int)card.CardRank >= 10) return 0;
            return (int)card.CardRank;
        }
    }
}
