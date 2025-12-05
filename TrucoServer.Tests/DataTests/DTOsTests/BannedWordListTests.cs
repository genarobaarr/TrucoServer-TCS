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
    public class BannedWordListTests
    {
        [TestMethod]
        public void TestConstructorInitializesBannedWordsList()
        {
            var bannedList = new BannedWordList();
            Assert.IsNotNull(bannedList.BannedWords);
        }

        [TestMethod]
        public void TestBannedWordsAddWordCountIncreases()
        {
            var bannedList = new BannedWordList();
            string word = "badword";
            bannedList.BannedWords.Add(word);
            Assert.AreEqual(1, bannedList.BannedWords.Count);
        }

        [TestMethod]
        public void TestBannedWordsSetNewListReturnsNewList()
        {
            var bannedList = new BannedWordList();
            
            var newList = new List<string> 
            {
                "one",
                "two" 
            };

            bannedList.BannedWords = newList;
            Assert.AreEqual(2, bannedList.BannedWords.Count);
        }

        [TestMethod]
        public void TestBannedWordsAddEmptyStringStoresEmptyString()
        {
            var bannedList = new BannedWordList();
            string emptyWord = "";
            bannedList.BannedWords.Add(emptyWord);
            Assert.IsTrue(bannedList.BannedWords.Contains(emptyWord));
        }

        [TestMethod]
        public void TestBannedWordsSetNullReturnsNull()
        {
            var bannedList = new BannedWordList();
            bannedList.BannedWords = null;
            Assert.IsNull(bannedList.BannedWords);
        }
    }
}
