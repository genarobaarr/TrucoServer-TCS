using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Profanity;

namespace TrucoServer.Tests.HelpersTests.ProfanityTests
{
    [TestClass]
    public class ProfanityServiceTests
    {
        private Mock<IBannedWordRepository> mockRepo;
        private ProfanityServerService service;

        [TestInitialize]
        public void Setup()
        {
            mockRepo = new Mock<IBannedWordRepository>();
            service = new ProfanityServerService(mockRepo.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorNullRepositoryShouldThrowException()
        {
            new ProfanityServerService(null);
        }

        [TestMethod]
        public void TestLoadBannedWordsValidSourceShouldPopulateCache()
        {
            var words = new List<string> 
            { 
                "badword1", 
                "badword2" 
            };

            mockRepo.Setup(r => r.GetAllWords()).Returns(words);
            service.LoadBannedWords();
            var result = service.GetBannedWordsForClient();

            Assert.AreEqual(2, result.BannedWords.Count);
            Assert.IsTrue(result.BannedWords.Contains("badword1"));
        }

        [TestMethod]
        public void TestLoadBannedWordsWithWhitespaceShouldTrimAndFilter()
        {
            var words = new List<string> 
            { 
                "  trimmed  ", 
                "", 
                "   ", 
                "valid" 
            };
            
            mockRepo.Setup(r => r.GetAllWords()).Returns(words);
            service.LoadBannedWords();
            var result = service.GetBannedWordsForClient();

            Assert.AreEqual(2, result.BannedWords.Count);
            Assert.IsTrue(result.BannedWords.Contains("trimmed"));
            Assert.IsTrue(result.BannedWords.Contains("valid"));
        }

        [TestMethod]
        public void TestContainsProfanityExactMatchShouldReturnTrue()
        {
            mockRepo.Setup(r => r.GetAllWords()).Returns(new List<string> { "bad" });
            service.LoadBannedWords();

            bool result = service.ContainsProfanity("This is bad");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestContainsProfanityCaseInsensitiveShouldReturnTrue()
        {
            mockRepo.Setup(r => r.GetAllWords()).Returns(new List<string> 
            { 
                "bad" 
            });

            service.LoadBannedWords();
            bool result = service.ContainsProfanity("This is BAD");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestContainsProfanityCleanTextShouldReturnFalse()
        {
            mockRepo.Setup(r => r.GetAllWords()).Returns(new List<string> 
            { 
                "bad" 
            });

            service.LoadBannedWords();
            bool result = service.ContainsProfanity("This is good");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestContainsProfanityEmptyCacheShouldReturnFalse()
        {
            mockRepo.Setup(r => r.GetAllWords()).Returns(new List<string>());
            service.LoadBannedWords();
            bool result = service.ContainsProfanity("bad");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestContainsProfanityNullInputShouldReturnFalse()
        {
            bool result = service.ContainsProfanity(null);
            Assert.IsFalse(result);
        }
    }
}
