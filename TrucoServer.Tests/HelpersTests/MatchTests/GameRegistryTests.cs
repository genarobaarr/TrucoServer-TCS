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
            var deck = new Mock<ITrucoDeck>();
            var gm = new Mock<IGameManager>();
            var cbs = new Dictionary<int, ITrucoCallback>();
            var players = new List<PlayerInformation>();

            gm.Setup(m => m.SaveMatchToDatabase(It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<List<PlayerInformation>>())).Returns(1);

            mockMatch = new TrucoMatch("CODE", 1, players, cbs, deck.Object, gm.Object);
        }

        [TestMethod]
        public void TestTryAddGameNewKeyShouldReturnTrue()
        {
            bool result = registry.TryAddGame("MATCH1", mockMatch);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestTryAddGameDuplicateKeyShouldReturnFalse()
        {
            registry.TryAddGame("MATCH1", mockMatch);
            bool result = registry.TryAddGame("MATCH1", mockMatch);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryGetGameExistingKeyShouldReturnTrue()
        {
            registry.TryAddGame("MATCH1", mockMatch);
            bool found = registry.TryGetGame("MATCH1", out _);
            Assert.IsTrue(found);
        }

        [TestMethod]
        public void TestTryGetGameExistingKeyShouldReturnCorrectInstance()
        {
            registry.TryAddGame("MATCH1", mockMatch);
            registry.TryGetGame("MATCH1", out var retrieved);
            Assert.AreSame(mockMatch, retrieved);
        }

        [TestMethod]
        public void TestTryRemoveGameExistingKeyShouldReturnTrue()
        {
            registry.TryAddGame("MATCH1", mockMatch);
            bool removed = registry.TryRemoveGame("MATCH1");
            Assert.IsTrue(removed);
        }

        [TestMethod]
        public void TestTryRemoveGameShouldActuallyRemoveItem()
        {
            registry.TryAddGame("MATCH1", mockMatch);
            registry.TryRemoveGame("MATCH1");
            bool foundAfterRemoval = registry.TryGetGame("MATCH1", out _);
            Assert.IsFalse(foundAfterRemoval);
        }
    }
}
