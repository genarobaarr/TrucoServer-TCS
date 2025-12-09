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

        [TestInitialize]
        public void Setup()
        {
            mockRepo = new Mock<IBannedWordRepository>();
        }

        [TestMethod]
        public void TestConstructorThrowsArgumentNullExceptionWhenRepoIsNull()
        {
            IBannedWordRepository nullRepo = null;
            Assert.ThrowsException<ArgumentNullException>(() => new ProfanityServerService(nullRepo));
        }

        [TestMethod]
        public void TestLoadBannedWordsTrimsAndIgnoresEmptyStringsFromRepo()
        {
            var dirtyList = new List<string> 
            {
                "  badword  ",
                string.Empty,
                null,
                "cleanword" 
            };

            mockRepo.Setup(r => r.GetAllWords()).Returns(dirtyList);
            var service = new ProfanityServerService(mockRepo.Object);

            service.LoadBannedWords();
            var result = service.GetBannedWordsForClient();
            Assert.AreEqual(2, result.BannedWords.Count);
        }

        [TestMethod]
        public void TestContainsProfanityReturnsTrueForCaseInsensitiveMatch()
        {
            mockRepo.Setup(r => r.GetAllWords()).Returns(new List<string> 
            { 
                "badword" 
            });

            var service = new ProfanityServerService(mockRepo.Object);
            service.LoadBannedWords();

            bool result = service.ContainsProfanity("This contains BaDwOrD inside");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestContainsProfanityReturnsFalseIfCacheIsEmpty()
        {
            mockRepo.Setup(r => r.GetAllWords()).Returns(new List<string>());
            var service = new ProfanityServerService(mockRepo.Object);
            service.LoadBannedWords();
            bool result = service.ContainsProfanity("badword");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoadBannedWordsHandlesRepositoryExceptionGracefully()
        {
            mockRepo.Setup(r => r.GetAllWords()).Throws(new Exception("DB Down"));
            var service = new ProfanityServerService(mockRepo.Object);
            service.LoadBannedWords();
            var result = service.GetBannedWordsForClient();
            Assert.AreEqual(0, result.BannedWords.Count);
        }
    }
}