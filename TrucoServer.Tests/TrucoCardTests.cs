using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class TrucoCardTests
    {
        private const string TEST_CLUB_1 = "club_1";
        private const string TEST_CUP_3 = "cup_3";
        private const string TEST_SWORD_7 = "sword_7";
        private const string TEST_GOLD_12 = "gold_12";
        private const string TEST_CLUB_12 = "club_12";
        private const string TEST_INVALID_SUIT_1 = "1";

        [TestMethod]
        public void TestConstructorSetsCardRankCorrectly()
        {
            var rank = Rank.Uno;
            var suit = Suit.Sword;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(rank, card.CardRank);
        }

        [TestMethod]
        public void TestConstructorSetsCardSuitCorrectly()
        {
            var rank = Rank.Uno;
            var suit = Suit.Cup;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(suit, card.CardSuit);
        }

        [TestMethod]
        public void TestFileNameIsGeneratedCorrectlyForClub()
        {
            var rank = Rank.Uno; 
            var suit = Suit.Club;
            string expected = TEST_CLUB_1;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(expected, card.FileName);
        }

        [TestMethod]
        public void TestFileNameIsGeneratedCorrectlyForCup()
        {
            var rank = Rank.Tres;
            var suit = Suit.Cup;
            string expected = TEST_CUP_3;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(expected, card.FileName);
        }

        [TestMethod]
        public void TestFileNameIsGeneratedCorrectlyForSword()
        {
            var rank = Rank.Siete;
            var suit = Suit.Sword;
            string expected = TEST_SWORD_7;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(expected, card.FileName);
        }

        [TestMethod]
        public void TestFileNameIsGeneratedCorrectlyForGold()
        {
            var rank = Rank.Doce; 
            var suit = Suit.Gold;
            string expected = TEST_GOLD_12;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(expected, card.FileName);
        }

        [TestMethod]
        public void TestFileNameIsGeneratedCorrectlyForBoundaryRank()
        {
            var rank = Rank.Doce;
            var suit = Suit.Club;
            string expected = TEST_CLUB_12;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(expected, card.FileName);
        }

        [TestMethod]
        public void TestFileNameHandleInvalidSuitGracefully()
        {
            var invalidSuit = (Suit)999;
            var rank = Rank.Uno;
            string expected = TEST_INVALID_SUIT_1;
            var card = new TrucoCard(rank, invalidSuit);
            Assert.AreEqual(expected, card.FileName);
        }
    }
}