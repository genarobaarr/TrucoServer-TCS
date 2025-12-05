using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.GameLogic;

namespace TrucoServer.Tests.GameLogic
{
    [TestClass]
    public class ActiveMatchTests
    {
        [TestMethod]
        public void TestPlayersInitializedAsEmptyList()
        {
            var match = new ActiveMatch();
            Assert.IsNotNull(match.Players);
        }

        [TestMethod]
        public void TestTableCardsInitializedAsEmptyList()
        {
            var match = new ActiveMatch();
            Assert.AreEqual(0, match.TableCards.Count);
        }

        [TestMethod]
        public void TestCodeSetValidStringReturnsString()
        {
            var match = new ActiveMatch();
            string code = "MATCHX";
            match.Code = code;
            Assert.AreEqual(code, match.Code);
        }
    }
}
