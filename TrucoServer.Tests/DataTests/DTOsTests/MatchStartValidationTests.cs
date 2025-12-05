using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Tests.DataTests.DTOsTests
{
    [TestClass]
    public class MatchStartValidationTests
    {
        [TestMethod]
        public void TestIsValidSetTrueReturnsTrue()
        {
            var validation = new MatchStartValidation();
            validation.IsValid = true;
            Assert.IsTrue(validation.IsValid);
        }

        [TestMethod]
        public void TestLobbyIdSetPositiveValueReturnsValue()
        {
            var validation = new MatchStartValidation();
            int id = 10;
            validation.LobbyId = id;
            Assert.AreEqual(id, validation.LobbyId);
        }

        [TestMethod]
        public void TestExpectedPlayersSetValueReturnsValue()
        {
            var validation = new MatchStartValidation();
            int count = 4;
            validation.ExpectedPlayers = count;
            Assert.AreEqual(count, validation.ExpectedPlayers);
        }

        [TestMethod]
        public void TestIsValidSetFalseReturnsFalse()
        {
            var validation = new MatchStartValidation();
            validation.IsValid = false;
            Assert.IsFalse(validation.IsValid);
        }

        [TestMethod]
        public void TestExpectedPlayersSetNegativeReturnsNegative()
        {
            var validation = new MatchStartValidation();
            int negative = -1;
            validation.ExpectedPlayers = negative;
            Assert.AreEqual(negative, validation.ExpectedPlayers);
        }
    }
}
