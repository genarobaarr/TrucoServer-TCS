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
        public void TestConstructorOneOfSwordShouldGenerateCorrectFileName()
        {
            Rank rank = Rank.Uno;
            Suit suit = Suit.Sword;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual("sword_1", card.FileName);
        }

        [TestMethod]
        public void TestConstructorSevenOfGoldShouldGenerateCorrectFileName()
        {
            Rank rank = Rank.Siete;
            Suit suit = Suit.Gold;
            var card = new TrucoCard(rank, suit);
            Assert.AreEqual("gold_7", card.FileName);
        }

        [TestMethod]
        public void TestRankEnumTenShouldHaveIntegerValueTen()
        {
            Rank rank = Rank.Diez;
            int value = (int)rank;
            Assert.AreEqual(10, value);
        }

        [TestMethod]
        public void TestCardSuitPropertyShouldStoreAssignedValue()
        {
            var card = new TrucoCard(Rank.Dos, Suit.Club);
            var suit = card.CardSuit;
            Assert.AreEqual(Suit.Club, suit);
        }

        [TestMethod]
        public void TestConstructorCupSuitShouldUseCupPrefix()
        {
            var card = new TrucoCard(Rank.Cuatro, Suit.Cup);
            Assert.IsTrue(card.FileName.StartsWith("cup_"));
        }
    }
}
