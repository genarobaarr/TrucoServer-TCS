using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.Entities;
using TrucoServer.Helpers.Profanity;

namespace TrucoServer.Tests.HelpersTests.ProfanityTests
{
    [TestClass]
    public class BannedWordRepositoryTests
    {
        private BannedWordRepository repository;

        [TestInitialize]
        public void Setup()
        {
            repository = new BannedWordRepository();
            using (var context = new baseDatosTrucoEntities())
            {
                context.Database.ExecuteSqlCommand("DELETE FROM BannedWord");
                context.BannedWord.Add(new BannedWord 
                { 
                    word = "IntegrationTestWord1" 
                });
                
                context.BannedWord.Add(new BannedWord
                { 
                    word = "IntegrationTestWord2" 
                });

                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                context.Database.ExecuteSqlCommand("DELETE FROM BannedWord");
            }
        }

        [TestMethod]
        public void TestGetAllWordsExistingDataShouldReturnList()
        {
            var result = repository.GetAllWords();

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains("IntegrationTestWord1"));
        }

        [TestMethod]
        public void TestGetAllWordsEmptyTableShouldReturnEmptyList()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                context.Database.ExecuteSqlCommand("DELETE FROM BannedWord");
            }

            var result = repository.GetAllWords();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }
    }
}
