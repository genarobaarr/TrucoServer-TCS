using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests
{
    [TestClass]
    public class DeckTests
    {
        private Mock<IDeckShuffler> mockShuffler;
        private Deck deck;

        [TestInitialize]
        public void Setup()
        {
            mockShuffler = new Mock<IDeckShuffler>();
            deck = new Deck(mockShuffler.Object);
        }

        [TestMethod]
        public void TestConstructorInitializesCorrectCardCount()
        {
            int expectedCount = 40;

            Assert.AreEqual(expectedCount, deck.RemainingCards);
        }

        [TestMethod]
        public void TestShuffleCallsShufflerShuffleMethod()
        {
            deck.Shuffle();

            mockShuffler.Verify(s => s.Shuffle(It.IsAny<List<TrucoCard>>()), Times.Once);
        }

        [TestMethod]
        public void TestDealHandReturnsThreeCards()
        {
            var hand = deck.DealHand();

            Assert.AreEqual(3, hand.Count);
        }

        [TestMethod]
        public void TestDealHandReducesRemainingCardsCount()
        {
            int initialCount = deck.RemainingCards;

            deck.DealHand();

            Assert.AreEqual(initialCount - 3, deck.RemainingCards);
        }

        [TestMethod]
        public void TestDealHandThrowsInvalidOperationExceptionWhenDeckIsLow()
        {
            while (deck.RemainingCards > 2)
            {
                deck.DealHand();
            }
            if (deck.RemainingCards > 0)
            {
                deck.DrawCard();
            }

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                deck.DealHand();
            });
        }

        [TestMethod]
        public void TestDrawCardReturnsNotNull()
        {
            var card = deck.DrawCard();

            Assert.IsNotNull(card);
        }

        [TestMethod]
        public void TestDrawCardReducesRemainingCardsCount()
        {
            int initialCount = deck.RemainingCards;

            deck.DrawCard();

            Assert.AreEqual(initialCount - 1, deck.RemainingCards);
        }

        [TestMethod]
        public void TestDrawCardThrowsInvalidOperationExceptionWhenDeckIsEmpty()
        {
            while (deck.RemainingCards > 0)
            {
                deck.DrawCard();
            }

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                deck.DrawCard();
            });
        }

        [TestMethod]
        public void TestResetRestoresCardCountToForty()
        {
            deck.DealHand(); 

            deck.Reset();

            Assert.AreEqual(40, deck.RemainingCards);
        }

        [TestMethod]
        public void TestResetDoesNotThrowException()
        {
            try
            {
                deck.Reset();
            }
            catch (Exception)
            {
                Assert.Fail("Reset should not throw exception under normal circumstances.");
            }
        }
    }

    [TestClass]
    public class DefaultDeckShufflerTests
    {
        [TestMethod]
        public void TestShuffleMaintainsListCount()
        {
            var shuffler = new DefaultDeckShuffler();
            var list = new List<int> { 1, 2, 3, 4, 5 };
            int expectedCount = list.Count;

            shuffler.Shuffle(list);

            Assert.AreEqual(expectedCount, list.Count);
        }

        [TestMethod]
        public void TestShufflePreservesElementsIntegrity()
        {
            var shuffler = new DefaultDeckShuffler();
            var list = new List<int> { 1, 2, 3 };

            shuffler.Shuffle(list);

            Assert.IsTrue(list.Contains(1));
        }

        [TestMethod]
        public void TestShuffleWithNullListDoesNotThrowException()
        {
            var shuffler = new DefaultDeckShuffler();

            try
            {
                shuffler.Shuffle<int>(null);
            }
            catch
            {
                Assert.Fail("Shuffle should catch ArgumentNullException and not rethrow it.");
            }
        }
    }
}