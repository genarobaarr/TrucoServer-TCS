using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests.GameLogic
{
    [TestClass]
    public class TrucoRulesTests
    {
        [TestMethod]
        public void TestCompareCardsReturnsOneWhenCardAIsHigher()
        {
            var cardA = new TrucoCard(Rank.Uno, Suit.Sword);
            var cardB = new TrucoCard(Rank.Cuatro, Suit.Gold);
            int result = TrucoRules.CompareCards(cardA, cardB);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestGetTrucoValueReturnsZeroForUnmappedCard()
        {
            var card = new TrucoCard((Rank)8, Suit.Sword);
            int value = TrucoRules.GetTrucoValue(card);
            Assert.AreEqual(0, value);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreWithTwoSameSuitReturnsCorrectSum()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Seis, Suit.Gold),
                new TrucoCard(Rank.Uno, Suit.Sword) 
            };

            int score = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(33, score);
        }

        [TestMethod]
        public void TestHasFlorReturnsTrueForThreeSameSuit()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Uno, Suit.Cup),
                new TrucoCard(Rank.Cinco, Suit.Cup),
                new TrucoCard(Rank.Siete, Suit.Cup)
            };

            bool hasFlor = TrucoRules.HasFlor(hand);
            Assert.IsTrue(hasFlor);
        }
    }
}
