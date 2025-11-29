using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Tests.HelpersTests.MatchTests
{
    [TestClass]
    public class MatchCodeGeneratorTests
    {
        private MatchCodeGenerator generator;

        [TestInitialize]
        public void Setup()
        {
            generator = new MatchCodeGenerator();
        }

        [TestMethod]
        public void TestGenerateMatchCodeShouldReturnSixCharString()
        {
            string code = generator.GenerateMatchCode();
            Assert.IsNotNull(code);
            Assert.AreEqual(6, code.Length);
        }

        [TestMethod]
        public void TestGenerateMatchCodeShouldBeAlphanumeric()
        {
            string code = generator.GenerateMatchCode();
            bool isAlphaNumeric = System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z0-9]+$");
            Assert.IsTrue(isAlphaNumeric, $"Code {code} contains invalid characters.");
        }

        [TestMethod]
        public void TestGenerateMatchCodeShouldGenerateUniqueValues()
        {
            var codes = new HashSet<string>();
           
            for (int i = 0; i < 100; i++)
            {
                codes.Add(generator.GenerateMatchCode());
            }

            Assert.AreEqual(100, codes.Count);
        }

        [TestMethod]
        public void TestGenerateNumericCodeFromStringShouldBeDeterministic()
        {
            string input = "ABC123";
            int hash1 = generator.GenerateNumericCodeFromString(input);
            int hash2 = generator.GenerateNumericCodeFromString(input);
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void TestGenerateNumericCodeFromStringShouldReturnPositiveInteger()
        {
            string input = "TEST";
            int hash = generator.GenerateNumericCodeFromString(input);
            Assert.IsTrue(hash >= 0);
        }
    }
}
