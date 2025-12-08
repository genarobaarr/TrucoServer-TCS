using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Utilities;

namespace TrucoServer.GameLogic
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
        private const int FLOR_POINTS = 20;

        private static readonly Dictionary<(Rank, Suit), int> trucoValueMap = new Dictionary<(Rank, Suit), int>
        {
            
            { 
                (Rank.Uno, Suit.Sword), FOURTEEN 
            }, 
            { 
                (Rank.Uno, Suit.Club), THIRTEEN
            },  
            { 
                (Rank.Siete, Suit.Sword), TWELVE
            },
            { 
                (Rank.Siete, Suit.Gold), ELEVEN
            },                
            
            { 
                (Rank.Tres, Suit.Sword), TEN 
            }, 
            { 
                (Rank.Tres, Suit.Club), TEN
            },
            { 
                (Rank.Tres, Suit.Cup), TEN
            }, 
            { 
                (Rank.Tres, Suit.Gold), TEN
            },
            
            { 
                (Rank.Dos, Suit.Sword), NINE
            }, 
            { 
                (Rank.Dos, Suit.Club), NINE
            },
            { 
                (Rank.Dos, Suit.Cup), NINE
            }, 
            { 
                (Rank.Dos, Suit.Gold), NINE
            }, 
            
            { 
                (Rank.Uno, Suit.Cup), EIGHT
            }, 
            { 
                (Rank.Uno, Suit.Gold), EIGHT
            }, 
            { 
                (Rank.Doce, Suit.Sword), SEVEN
            }, 
            { 
                (Rank.Doce, Suit.Club), SEVEN
            },
            { 
                (Rank.Doce, Suit.Cup), SEVEN
            }, 
            { 
                (Rank.Doce, Suit.Gold), SEVEN
            }, 
            { 
                (Rank.Once, Suit.Sword), SIX
            }, 
            { 
                (Rank.Once, Suit.Club), SIX
            },
            { 
                (Rank.Once, Suit.Cup), SIX
            }, 
            { 
                (Rank.Once, Suit.Gold), SIX
            },
            { 
                (Rank.Diez, Suit.Sword), FIVE
            }, 
            { 
                (Rank.Diez, Suit.Club), FIVE
            },
            { 
                (Rank.Diez, Suit.Cup), FIVE
            }, 
            { 
                (Rank.Diez, Suit.Gold), FIVE
            }, 
            
            { 
                (Rank.Siete, Suit.Club), FOUR
            }, 
            { 
                (Rank.Siete, Suit.Cup), FOUR
            }, 
            
            { 
                (Rank.Seis, Suit.Sword), THREE 
            }, 
            { 
                (Rank.Seis, Suit.Club), THREE
            },
            { 
                (Rank.Seis, Suit.Cup), THREE
            }, 
            { 
                (Rank.Seis, Suit.Gold), THREE
            }, 
            
            { 
                (Rank.Cinco, Suit.Sword), TWO
            }, 
            { 
                (Rank.Cinco, Suit.Club), TWO 
            },
            { 
                (Rank.Cinco, Suit.Cup), TWO
            }, 
            { 
                (Rank.Cinco, Suit.Gold), TWO 
            }, 
            { 
                (Rank.Cuatro, Suit.Sword), ONE
            }, 
            { 
                (Rank.Cuatro, Suit.Club), ONE
            },
            { 
                (Rank.Cuatro, Suit.Cup), ONE
            }, 
            { 
                (Rank.Cuatro, Suit.Gold), ONE
            }
        };

        public static int GetTrucoValue(TrucoCard card)
        {
            try
            {
                if (card == null)
                {
                    throw new ArgumentNullException(nameof(card));
                }

                if (trucoValueMap.TryGetValue((card.CardRank, card.CardSuit), out int value))
                {
                    return value;
                }

                LogManager.LogError(new InvalidOperationException($"Unmapped card: {card.FileName}"), nameof(GetTrucoValue));

                return 0;
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(GetTrucoValue));
                return 0;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetTrucoValue));
                return 0;
            }
        }

        public static int CompareCards(TrucoCard cardA, TrucoCard cardB)
        {
            try
            {
                int valueA = GetTrucoValue(cardA);
                int valueB = GetTrucoValue(cardB);

                if (valueA > valueB)
                {
                    return 1;
                }

                if (valueB > valueA)
                {
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CompareCards));
                return 0;
            }
        }

        public static int CalculateEnvidoScore(List<TrucoCard> hand)
        {
            try
            {
                if (hand == null)
                {
                    throw new ArgumentNullException(nameof(hand));
                }

                var groups = hand.GroupBy(card => card.CardSuit);
                var bestGroup = groups.OrderByDescending(g => g.Count()).FirstOrDefault();

                if (bestGroup == null || bestGroup.Count() < 2)
                {
                    if (!hand.Any())
                    {
                        return 0;
                    }
                    return hand.Max(card => GetEnvidoValue(card));
                }
                else
                {
                    var twoHighest = bestGroup.OrderByDescending(card => GetEnvidoValue(card)).Take(2).ToList();

                    return GetEnvidoValue(twoHighest[0]) + GetEnvidoValue(twoHighest[1]) + 20;
                }
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(CalculateEnvidoScore));
                return 0;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CalculateEnvidoScore));
                return 0;
            }
        }

        public static bool HasFlor(List<TrucoCard> hand)
        {
            try
            {
                if (hand == null || hand.Count < 3)
                {
                    return false;
                }

                return hand.GroupBy(card => card.CardSuit).Any(g => g.Count() >= 3);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(HasFlor));
                return false;
            }
        }

        public static int CalculateFlorScore(List<TrucoCard> hand)
        {
            try
            {
                if (!HasFlor(hand))
                {
                    return 0;
                }

                int sumValues = hand.Sum(card => GetEnvidoValue(card));

                return sumValues + FLOR_POINTS;
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(CalculateFlorScore));
                return 0;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CalculateFlorScore));
                return 0;
            }
        }

        public static int GetEnvidoValue(TrucoCard card)
        {
            try
            {
                if ((int)card.CardRank >= 10)
                {
                    return 0;
                }

                return (int)card.CardRank;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetEnvidoValue));
                return 0;
            }
        }
    }
}
