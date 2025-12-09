using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.Entities;

namespace TrucoServer.Tests.DataTests.EntititesTests
{
    [TestClass]
    public class BannedWordEntityTests
    {
        private const int USER_ID = 10;
        private const int WORD_ID = 0;
        private const int LONG_WORD = 50;

        [TestMethod]
        public void TestWordIDSetPositiveIntegerReturnsInteger()
        {
            var entity = new BannedWordEntity();
            int userId = USER_ID;
            entity.WordID = userId;
            Assert.AreEqual(userId, entity.WordID);
        }

        [TestMethod]
        public void TestWordSetValidStringReturnsString()
        {
            var entity = new BannedWordEntity();
            string word = "forbidden";
            entity.Word = word;
            Assert.AreEqual(word, entity.Word);
        }

        [TestMethod]
        public void TestWordSetNullReturnsNull()
        {
            var entity = new BannedWordEntity();
            entity.Word = null;
            Assert.IsNull(entity.Word);
        }

        [TestMethod]
        public void TestWordIDSetZeroReturnsZero()
        {
            var entity = new BannedWordEntity();
            entity.WordID = WORD_ID;
            Assert.AreEqual(0, entity.WordID);
        }

        [TestMethod]
        public void TestWordSetMaxLengthStringReturnsString()
        {
            var entity = new BannedWordEntity();
            string longWord = new string('x', LONG_WORD);
            entity.Word = longWord;
            Assert.AreEqual(50, entity.Word.Length);
        }
    }
}
