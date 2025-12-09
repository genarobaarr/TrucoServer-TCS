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
    public class DeckTests
    {
        private const int CARDS = 40;

        [TestMethod]
        public void TestRemainingCardsInitializesWith40Cards()
        {
            var deck = new Deck();
            Assert.AreEqual(CARDS, deck.RemainingCards);
        }

        [TestMethod]
        public void TestDealHandReducesDeckCountByThree()
        {
            var deck = new Deck();
            int initialCount = deck.RemainingCards;
            deck.DealHand();
            Assert.AreEqual(initialCount - 3, deck.RemainingCards);
        }

        [TestMethod]
        public void TestDrawCardReducesDeckCountByOne()
        {
            var deck = new Deck();
            int initialCount = deck.RemainingCards;
            deck.DrawCard();
            Assert.AreEqual(initialCount - 1, deck.RemainingCards);
        }

        [TestMethod]
        public void TestResetRestoresDeckTo40Cards()
        {
            var deck = new Deck();
            deck.DealHand();
            deck.DealHand();
            deck.Reset();
            Assert.AreEqual(CARDS, deck.RemainingCards);
        }

        [TestMethod]
        public void TestDrawCardReturnsNullWhenDeckEmpty()
        {
            var deck = new Deck();
           
            for (int i = 0; i < 13; i++)
            {
                deck.DealHand();
            }

            deck.DrawCard();
            var card = deck.DrawCard();
            Assert.IsNull(card);
        }
    }
}
