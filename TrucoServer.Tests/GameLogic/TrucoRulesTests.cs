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
        public void TestGetTrucoValueAceOfSwordsShouldReturnFourteen()
        {
            var card = new TrucoCard(Rank.Uno, Suit.Sword);
            int value = TrucoRules.GetTrucoValue(card);
            Assert.AreEqual(14, value);
        }

        [TestMethod]
        public void TestGetTrucoValueFourOfClubsShouldReturnOne()
        {
            var card = new TrucoCard(Rank.Cuatro, Suit.Club); 
            int value = TrucoRules.GetTrucoValue(card);
            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void TestCompareCardsHigherValueShouldWin()
        {
            var winner = new TrucoCard(Rank.Uno, Suit.Sword);
            var loser = new TrucoCard(Rank.Uno, Suit.Cup);
            int result = TrucoRules.CompareCards(winner, loser);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestCompareCardsEqualValueShouldTie()
        {
            var card1 = new TrucoCard(Rank.Tres, Suit.Gold);
            var card2 = new TrucoCard(Rank.Tres, Suit.Club);
            int result = TrucoRules.CompareCards(card1, card2);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreTwoCardsSameSuitShouldSumValuesPlusTwenty()
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
        public void TestCalculateEnvidoScoreFaceCardsShouldCountAsZero()
        {
            var hand = new List<TrucoCard>
        {
            new TrucoCard(Rank.Once, Suit.Cup),
            new TrucoCard(Rank.Doce, Suit.Cup),
            new TrucoCard(Rank.Uno, Suit.Sword)
        };

            int score = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(20, score);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreNoMatchingSuitsShouldReturnHighestSingleCard()
        {
            var hand = new List<TrucoCard>
        {
            new TrucoCard(Rank.Siete, Suit.Gold),
            new TrucoCard(Rank.Uno, Suit.Sword),
            new TrucoCard(Rank.Tres, Suit.Club)
        };

            int score = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(7, score);
        }

        [TestMethod]
        public void TestHasFlorThreeCardsSameSuitShouldReturnTrue()
        {
            var hand = new List<TrucoCard>
        {
            new TrucoCard(Rank.Uno, Suit.Cup),
            new TrucoCard(Rank.Cinco, Suit.Cup),
            new TrucoCard(Rank.Doce, Suit.Cup)
        };

            bool hasFlor = TrucoRules.HasFlor(hand);
            Assert.IsTrue(hasFlor);
        }

        [TestMethod]
        public void TestCalculateFlorScoreThreeCardsSameSuitShouldSumValuesPlusTwenty()
        {
            var hand = new List<TrucoCard>
        {
            new TrucoCard(Rank.Tres, Suit.Sword),
            new TrucoCard(Rank.Cinco, Suit.Sword),
            new TrucoCard(Rank.Diez, Suit.Sword)
        };

            int score = TrucoRules.CalculateFlorScore(hand);
            Assert.AreEqual(28, score);
        }
    }
}
