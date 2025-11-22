using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class TrucoRulesTests
    {
        private const int TEST_CARD_A_VALUE = 14;
        private const int TEST_DRAW = 0;
        private const int TEST_ENVIDO = 33;
        private const int TEST_CARD_A_LOSS = -1;
        private const int TEST_CARD_A_WIN = 1;
        private const int TEST_CARD_B_WIN = 0;
        private const int TEST_FLOWER = 38;
        private const int TEST_RETURN_SEVEN = 7;

        [TestMethod]
        public void TestGetTrucoValueReturnsFourteenForAceOfSwords()
        {
            var card = new TrucoCard(Rank.Uno, Suit.Sword);
            int result = TrucoRules.GetTrucoValue(card);
            Assert.AreEqual(TEST_CARD_A_VALUE, result);
        }

        [TestMethod]
        public void TestGetTrucoValueReturnsOneForFourOfGold()
        {
            var card = new TrucoCard(Rank.Cuatro, Suit.Gold);
            int result = TrucoRules.GetTrucoValue(card);
            Assert.AreEqual(TEST_CARD_A_WIN, result);
        }

        [TestMethod]
        public void TestGetTrucoValueReturnsZeroForNullCard()
        {
            TrucoCard card = null;
            int result = TrucoRules.GetTrucoValue(card);
            Assert.AreEqual(TEST_CARD_B_WIN, result);
        }

        [TestMethod]
        public void TestCompareCardsReturnsOneWhenFirstCardIsHigher()
        {
            var cardA = new TrucoCard(Rank.Uno, Suit.Sword);
            var cardB = new TrucoCard(Rank.Cuatro, Suit.Gold);
            int result = TrucoRules.CompareCards(cardA, cardB);
            Assert.AreEqual(TEST_CARD_A_WIN, result);
        }

        [TestMethod]
        public void TestCompareCardsReturnsMinusOneWhenSecondCardIsHigher()
        {
            var cardA = new TrucoCard(Rank.Cuatro, Suit.Gold);
            var cardB = new TrucoCard(Rank.Uno, Suit.Sword);
            int result = TrucoRules.CompareCards(cardA, cardB);
            Assert.AreEqual(TEST_CARD_A_LOSS, result);
        }

        [TestMethod]
        public void TestCompareCardsReturnsZeroWhenCardsAreEqualValue()
        {
            var cardA = new TrucoCard(Rank.Tres, Suit.Gold);
            var cardB = new TrucoCard(Rank.Tres, Suit.Cup);
            int result = TrucoRules.CompareCards(cardA, cardB);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestCompareCardsReturnsZeroForNullInputs()
        {
            TrucoCard cardA = null;
            TrucoCard cardB = null;
            int result = TrucoRules.CompareCards(cardA, cardB);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestGetEnvidoValueReturnsRankValueForSeven()
        {
            var card = new TrucoCard(Rank.Siete, Suit.Sword);
            int result = TrucoRules.GetEnvidoValue(card);
            Assert.AreEqual(TEST_RETURN_SEVEN, result);
        }

        [TestMethod]
        public void TestGetEnvidoValueReturnsZeroForTen()
        {
            var card = new TrucoCard(Rank.Diez, Suit.Gold);
            int result = TrucoRules.GetEnvidoValue(card);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestGetEnvidoValueReturnsZeroForNullCard()
        {
            TrucoCard card = null;
            int result = TrucoRules.GetEnvidoValue(card);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestHasFlorReturnsTrueForThreeCardsOfSameSuit()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Uno, Suit.Gold),
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Cinco, Suit.Gold)
            };
            bool result = TrucoRules.HasFlor(hand);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestHasFlorReturnsFalseForDifferentSuits()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Uno, Suit.Gold),
                new TrucoCard(Rank.Siete, Suit.Sword),
                new TrucoCard(Rank.Cinco, Suit.Cup)
            };
            bool result = TrucoRules.HasFlor(hand);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestHasFlorReturnsFalseForNullHand()
        {
            List<TrucoCard> hand = null;
            bool result = TrucoRules.HasFlor(hand);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestHasFlorReturnsFalseForInsufficientCards()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Uno, Suit.Gold),
                new TrucoCard(Rank.Siete, Suit.Gold)
            };
            bool result = TrucoRules.HasFlor(hand);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreCalculatesCorrectlyForPair()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Seis, Suit.Gold),
                new TrucoCard(Rank.Uno, Suit.Sword)
            };
            int result = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(TEST_ENVIDO, result);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreUsesTwoHighestCardsInFlor()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Seis, Suit.Gold),
                new TrucoCard(Rank.Cinco, Suit.Gold)
            };
            int result = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(TEST_ENVIDO, result);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreReturnsHighestRankWhenNoPairExists()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Cuatro, Suit.Sword),
                new TrucoCard(Rank.Dos, Suit.Cup)
            };
            int result = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(TEST_RETURN_SEVEN, result);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreReturnsZeroForNullHand()
        {
            List<TrucoCard> hand = null;
            int result = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestCalculateEnvidoScoreReturnsZeroForEmptyHand()
        {
            var hand = new List<TrucoCard>();
            int result = TrucoRules.CalculateEnvidoScore(hand);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestCalculateFlorScoreReturnsCorrectSumPlusTwenty()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Seis, Suit.Gold),
                new TrucoCard(Rank.Cinco, Suit.Gold)
            };
            int result = TrucoRules.CalculateFlorScore(hand);
            Assert.AreEqual(TEST_FLOWER, result);
        }

        [TestMethod]
        public void TestCalculateFlorScoreReturnsZeroWhenNoFlorExists()
        {
            var hand = new List<TrucoCard>
            {
                new TrucoCard(Rank.Siete, Suit.Gold),
                new TrucoCard(Rank.Seis, Suit.Sword),
                new TrucoCard(Rank.Cinco, Suit.Gold)
            };
            int result = TrucoRules.CalculateFlorScore(hand);
            Assert.AreEqual(TEST_DRAW, result);
        }

        [TestMethod]
        public void TestCalculateFlorScoreReturnsZeroForNullHand()
        {
            List<TrucoCard> hand = null;
            int result = TrucoRules.CalculateFlorScore(hand);
            Assert.AreEqual(TEST_DRAW, result);
        }
    }
}