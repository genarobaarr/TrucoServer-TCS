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
        [TestMethod]
        public void TestBannedWordEntitySetWordShouldStoreValue()
        {
            var entity = new BannedWordEntity();
            string badWord = "Forbidden";
            entity.Word = badWord;
            Assert.AreEqual(badWord, entity.Word);
        }

        [TestMethod]
        public void TestBannedWordEntityValidationRequiredFieldShouldFailIfNull()
        {
            var entity = new BannedWordEntity 
            { 
                Word = null 
            };

            var context = new ValidationContext(entity);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(entity, context, results, true);
            Assert.IsFalse(isValid, "Validation should fail because Word is [Required]");
        }
    }
}
