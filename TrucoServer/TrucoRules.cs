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
        private const int ZERO = 0;
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
        private const int TWELVE = 12;

        private static readonly Dictionary<(Rank, Suit), int> trucoValueMap = new Dictionary<(Rank, Suit), int>
        {
            
            // Cartas más altas
            { 
                (Rank.Uno, Suit.sword_), TWELVE 
            }, 
            { 
                (Rank.Uno, Suit.club_), TWELVE
            },  
            { 
                (Rank.Siete, Suit.sword_), TEN
            },
            { 
                (Rank.Siete, Suit.gold_), NINE 
            },                
            
            // Cartas del resto de la jerarquía
            { 
                (Rank.Tres, Suit.sword_), EIGHT 
            }, 
            { 
                (Rank.Tres, Suit.club_), EIGHT
            },
            { 
                (Rank.Tres, Suit.cup_), EIGHT
            }, 
            { 
                (Rank.Tres, Suit.gold_), EIGHT 
            },
            
            { 
                (Rank.Dos, Suit.sword_), SEVEN 
            }, 
            { 
                (Rank.Dos, Suit.club_), SEVEN
            },
            { 
                (Rank.Dos, Suit.cup_), SEVEN
            }, 
            { 
                (Rank.Dos, Suit.gold_), SEVEN
            }, 
            
            { 
                (Rank.Uno, Suit.cup_), SIX
            }, 
            { 
                (Rank.Uno, Suit.gold_), SIX
            }, 
            { 
                (Rank.Doce, Suit.sword_), FIVE
            }, 
            { 
                (Rank.Doce, Suit.club_), FIVE
            },
            { 
                (Rank.Doce, Suit.cup_), FIVE
            }, 
            { 
                (Rank.Doce, Suit.gold_), FIVE
            }, 
            { 
                (Rank.Once, Suit.sword_), FOUR
            }, 
            { 
                (Rank.Once, Suit.club_), FOUR
            },
            { 
                (Rank.Once, Suit.cup_), FOUR
            }, 
            { 
                (Rank.Once, Suit.gold_), FOUR
            },
            { 
                (Rank.Diez, Suit.sword_), THREE
            }, 
            { 
                (Rank.Diez, Suit.club_), THREE
            },
            { 
                (Rank.Diez, Suit.cup_), THREE
            }, 
            { 
                (Rank.Diez, Suit.gold_), THREE
            }, 
            
            // 7s falsos (los de basto y copa)
            { 
                (Rank.Siete, Suit.club_), TWO
            }, 
            { 
                (Rank.Siete, Suit.cup_), TWO 
            }, 
            
            // Cartas más bajas (6, 5 y 4)
            { 
                (Rank.Seis, Suit.sword_), ONE 
            }, 
            { 
                (Rank.Seis, Suit.club_), ONE 
            },
            { 
                (Rank.Seis, Suit.cup_), ONE 
            }, 
            { 
                (Rank.Seis, Suit.gold_), ONE 
            }, 
            
            // Valores mínimos
            { 
                (Rank.Cinco, Suit.sword_), ZERO 
            }, 
            { 
                (Rank.Cinco, Suit.club_), ZERO 
            },
            { 
                (Rank.Cinco, Suit.cup_), ZERO
            }, 
            { 
                (Rank.Cinco, Suit.gold_), ZERO 
            }, 
            { 
                (Rank.Cuatro, Suit.sword_), ZERO
            }, 
            { 
                (Rank.Cuatro, Suit.club_), ZERO
            },
            { (Rank.Cuatro, Suit.cup_), ZERO
            }, 
            { 
                (Rank.Cuatro, Suit.gold_), ZERO
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

        // Aquí iría la lógica para calcular el Envido/Flor
        public static int GetEnvidoValue(TrucoCard card)
        {
            if ((int)card.CardRank >= 10) return 0;
            return (int)card.CardRank;
        }
    }
}
