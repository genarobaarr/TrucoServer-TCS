using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class GameRegistryTests
    {
        private GameRegistry registry;
        private TrucoMatch mockMatch;

        [TestInitialize]
        public void Setup()
        {
            registry = new GameRegistry();
            var mockGm = new Mock<IGameManager>();
            var mockDeck = new Mock<ITrucoDeck>();
            var players = new List<PlayerInformation>();
            var callbacks = new Dictionary<int, ITrucoCallback>();

            var context = new TrucoMatchContext
            {
                MatchCode = "TEST01",
                LobbyId = 1,
                Players = players,
                Callbacks = callbacks,
                Deck = mockDeck.Object,
                GameManager = mockGm.Object
            };

            mockMatch = new TrucoMatch(context);
        }

        [TestMethod]
        public void TestTryAddGameReturnsTrueForNewCode()
        {
            bool result = registry.TryAddGame("TEST01", mockMatch);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestTryAddGameReturnsFalseForDuplicateCode()
        {
            registry.TryAddGame("TEST01", mockMatch);
            bool result = registry.TryAddGame("TEST01", mockMatch);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryGetGameReturnsTrueIfGameExists()
        {
            registry.TryAddGame("TEST01", mockMatch);
            bool found = registry.TryGetGame("TEST01", out _);
            Assert.IsTrue(found);
        }

        [TestMethod]
        public void TestTryGetGameReturnsCorrectInstanceIfGameExists()
        {
            registry.TryAddGame("TEST01", mockMatch);
            registry.TryGetGame("TEST01", out TrucoMatch retrieved);
            Assert.AreSame(mockMatch, retrieved);
        }

        [TestMethod]
        public void TestTryRemoveGameReturnsTrueWhenRemoved()
        {
            registry.TryAddGame("TEST01", mockMatch);
            bool removed = registry.TryRemoveGame("TEST01");
            Assert.IsTrue(removed);
        }

        [TestMethod]
        public void TestTryRemoveGameEffectivelyRemovesMatch()
        {
            registry.TryAddGame("TEST01", mockMatch);
            registry.TryRemoveGame("TEST01");
            bool found = registry.TryGetGame("TEST01", out _);
            Assert.IsFalse(found);
        }
    }
}
