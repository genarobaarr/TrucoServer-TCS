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
        public void TestActiveMatchConstructorShouldInitializePlayersList()
        {
            var match = new ActiveMatch();
            Assert.IsNotNull(match.Players);
        }

        [TestMethod]
        public void TestActiveMatchConstructorShouldInitializeTableCardsList()
        {
            var match = new ActiveMatch();
            Assert.IsNotNull(match.TableCards);
        }

        [TestMethod]
        public void TestActiveMatchConstructorShouldInitializeEmptyPlayersList()
        {
            var match = new ActiveMatch();
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
