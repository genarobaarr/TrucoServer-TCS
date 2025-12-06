using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Security;
using TrucoServer.Utilities;

namespace TrucoServer.Tests.HelpersTests.AuthenticationTests
{
    [TestClass]
    public class UserAuthenticationHelperTests
    {
        [TestMethod]
        public void TestGenerateSecureNumericCodeReturnsStringOfLengthSix()
        {
            var helper = new UserAuthenticationHelper();
            string code = helper.GenerateSecureNumericCode();
            Assert.AreEqual(6, code.Length);
        }

        [TestMethod]
        public void TestGenerateSecureNumericCodeReturnsDigitsOnly()
        {
            var helper = new UserAuthenticationHelper();
            string code = helper.GenerateSecureNumericCode();
            Assert.IsTrue(int.TryParse(code, out _));
        }

        [TestMethod]
        public void TestGenerateSecureNumericCodeIsRandomEnough()
        {
            var helper = new UserAuthenticationHelper();
            var codes = new HashSet<string>();

            for (int i = 0; i < 100; i++)
            {
                codes.Add(helper.GenerateSecureNumericCode());
            }

            Assert.IsTrue(codes.Count > 90);
        }
    }
}

