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
    public class TrucoCardTests
    {
        [TestMethod]
        public void TestConstructorSetsRankCorrectly()
        {
            Rank rank = Rank.Cinco;
            Suit suit = Suit.Gold;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(rank, card.CardRank);
        }

        [TestMethod]
        public void TestConstructorSetsSuitCorrectly()
        {
            Rank rank = Rank.Uno;
            Suit suit = Suit.Sword;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual(suit, card.CardSuit);
        }

        [TestMethod]
        public void TestConstructorGeneratesFileNameForGold()
        {
            var card = new TrucoCard(Rank.Siete, Suit.Gold);
            string expected = "gold_7";
            string fileName = card.FileName;
            Assert.AreEqual(expected, fileName);
        }

        [TestMethod]
        public void TestConstructorGeneratesFileNameForClub()
        {
            var card = new TrucoCard(Rank.Doce, Suit.Club);
            string expected = "club_12";
            string fileName = card.FileName;
            Assert.AreEqual(expected, fileName);
        }

        [TestMethod]
        public void TestCardRankSetNewValueReturnsValue()
        {
            var card = new TrucoCard(Rank.Uno, Suit.Cup);
            Rank newRank = Rank.Tres;
            card.CardRank = newRank;
            Assert.AreEqual(newRank, card.CardRank);
        }
    }
}
