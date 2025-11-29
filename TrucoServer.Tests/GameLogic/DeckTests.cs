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
        [TestMethod]
        public void TestConstructorDefaultShouldInitialize40Cards()
        {
            var deck = new Deck();
            Assert.AreEqual(40, deck.RemainingCards, "A trick deck should start with 40 cards");
        }

        [TestMethod]
        public void TestDealHandDeckFullShouldReturnThreeCards()
        {
            var deck = new Deck();
            int initialCount = deck.RemainingCards;
            var hand = deck.DealHand();
            Assert.AreEqual(3, hand.Count);
            Assert.AreEqual(initialCount - 3, deck.RemainingCards);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDealHandNotEnoughCardsShouldThrowException()
        {
            var deck = new Deck();
            
            while (deck.RemainingCards > 0)
            {
                deck.DrawCard();
            }

            deck.DealHand();
        }

        [TestMethod]
        public void TestResetAfterDealingShouldRestoreCountTo40()
        {
            var deck = new Deck();
            deck.DealHand();
            deck.Reset();
            Assert.AreEqual(40, deck.RemainingCards);
        }

        [TestMethod]
        public void TestDrawCardSingleCardShouldReduceCountByOne()
        {
            var deck = new Deck();
            var card = deck.DrawCard();
            Assert.AreEqual(39, deck?.RemainingCards);
        }
    }
}
