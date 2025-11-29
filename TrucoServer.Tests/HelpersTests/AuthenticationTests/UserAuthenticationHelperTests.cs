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
        private UserAuthenticationHelper helper;
        private const string TEST_USER = "TestUserProMax";
        private const string TEST_PASS = "PasswordTest";

        [TestInitialize]
        public void Setup()
        {
            helper = new UserAuthenticationHelper();

            using (var context = new baseDatosTrucoEntities())
            {
                var existing = context.User.FirstOrDefault(u => u.username == TEST_USER);
                
                if (existing != null)
                {
                    context.User.Remove(existing);
                    context.SaveChanges();
                }

                string hash = PasswordHasher.Hash(TEST_PASS);
               
                var user = new User
                {
                    username = TEST_USER,
                    email = "a@gmail.com",
                    passwordHash = hash,
                    nameChangeCount = 0,
                };
                
                context.User.Add(user);
                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == TEST_USER);
                
                if (user != null)
                {
                    context.User.Remove(user);
                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestAuthenticateUserCorrectCredentialsShouldReturnUserObject()
        {
            var user = helper.AuthenticateUser(TEST_USER, TEST_PASS);
            Assert.AreEqual(TEST_USER, user.username);
        }

        [TestMethod]
        public void TestAuthenticateUserWrongPasswordShouldReturnNull()
        {
            var user = helper.AuthenticateUser(TEST_USER, "WrongPass");
            Assert.IsNull(user);
        }

        [TestMethod]
        public void TestAuthenticateUserNonExistentUserShouldReturnNull()
        {
            var user = helper.AuthenticateUser("TestUser", "AnyPass");
            Assert.IsNull(user);
        }

        [TestMethod]
        public void TestGenerateSecureNumericCodeShouldReturnSixDigits()
        {
            string code = helper.GenerateSecureNumericCode();
            Assert.AreEqual(6, code.Length);
        }

        [TestMethod]
        public void TestGenerateSecureNumericCodeShouldReturnTrue()
        {
            string code = helper.GenerateSecureNumericCode();
            Assert.IsTrue(int.TryParse(code, out _));
        }

        [TestMethod]
        public void TestValidateBruteForceStatusCleanUserShouldNotThrow()
        {
            helper.ValidateBruteForceStatus("NewUser123");
        }
    }
}

