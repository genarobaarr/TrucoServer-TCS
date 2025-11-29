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
        public void TestActiveMatchConstructorShouldInitializeLists()
        {
            var match = new ActiveMatch();
            Assert.IsNotNull(match.Players, "The list of players must not be null");
            Assert.IsNotNull(match.TableCards, "The list of cards on the table must not be null");
            Assert.AreEqual(0, match.Players.Count);
        }

        [TestMethod]
        public void TestActiveMatchSetCodeShouldStoreValue()
        {
            var match = new ActiveMatch();
            string code = "1234K";
            match.Code = code;
            Assert.AreEqual(code, match.Code);
        }
    }
}
