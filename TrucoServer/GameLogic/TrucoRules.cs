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
        private const int ENVIDO_BONUS = 20;

        private const int ZERO = 0;
        private const int TWO_CARDS = 2;
        private const int THREE_CARDS = 3;

        private const int ENVIDO_RANK_LIMIT = 10;

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

                ServerException.HandleException(new InvalidOperationException($"Unmapped card: {card.FileName}"), nameof(GetTrucoValue));

                return ZERO;
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(GetTrucoValue));
              
                return ZERO;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetTrucoValue));
               
                return ZERO;
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
                    return ONE;
                }

                if (valueB > valueA)
                {
                    return -ONE;
                }

                return ZERO;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CompareCards));
               
                return ZERO;
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

                if (bestGroup == null || bestGroup.Count() < TWO_CARDS)
                {
                    if (!hand.Any())
                    {
                        return ZERO;
                    }
                    return hand.Max(card => GetEnvidoValue(card));
                }
                else
                {
                    var twoHighest = bestGroup.OrderByDescending(card => GetEnvidoValue(card))
                                              .Take(TWO_CARDS)
                                              .ToList();

                    return GetEnvidoValue(twoHighest[0]) +
                           GetEnvidoValue(twoHighest[1]) +
                           ENVIDO_BONUS;
                }
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(CalculateEnvidoScore));
                
                return ZERO;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CalculateEnvidoScore));
                
                return ZERO;
            }
        }

        public static bool HasFlor(List<TrucoCard> hand)
        {
            try
            {
                if (hand == null || hand.Count < THREE_CARDS)
                {
                    return false;
                }

                return hand.GroupBy(card => card.CardSuit)
                           .Any(g => g.Count() >= THREE_CARDS);
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
                    return ZERO;
                }

                int sumValues = hand.Sum(card => GetEnvidoValue(card));

                return sumValues + FLOR_POINTS;
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(CalculateFlorScore));
               
                return ZERO;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(CalculateFlorScore));
                
                return ZERO;
            }
        }

        public static int GetEnvidoValue(TrucoCard card)
        {
            try
            {
                if ((int)card.CardRank >= ENVIDO_RANK_LIMIT)
                {
                    return ZERO;
                }

                return (int)card.CardRank;
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetEnvidoValue));
                
                return ZERO;
            }
        }
    }
}
