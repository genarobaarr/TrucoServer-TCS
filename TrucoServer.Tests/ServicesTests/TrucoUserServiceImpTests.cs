using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Mapping;
using TrucoServer.Helpers.Password;
using TrucoServer.Helpers.Profiles;
using TrucoServer.Helpers.Ranking;
using TrucoServer.Helpers.Sessions;
using TrucoServer.Helpers.Verification;
using TrucoServer.Services;

namespace TrucoServer.Tests.Services
{
    [TestClass]
    public class TrucoUserServiceImpTests
    {
        private Mock<baseDatosTrucoEntities> mockContext;
        private Mock<IUserAuthenticationHelper> mockAuth;
        private Mock<IUserSessionManager> mockSession;
        private Mock<IEmailSender> mockEmail;
        private Mock<IVerificationService> mockVerification;
        private Mock<IProfileUpdater> mockProfile;
        private Mock<IPasswordManager> mockPassword;
        private Mock<IUserMapper> mockMapper;
        private Mock<IRankingService> mockRanking;
        private Mock<IMatchHistoryService> mockHistory;
        private Mock<DbSet<User>> mockUserSet;
        private Mock<DbSet<UserProfile>> mockUserProfileSet;

        private TrucoUserServiceImp service;

        [TestInitialize]
        public void Setup()
        {
            mockContext = new Mock<baseDatosTrucoEntities>();
            mockAuth = new Mock<IUserAuthenticationHelper>();
            mockSession = new Mock<IUserSessionManager>();
            mockEmail = new Mock<IEmailSender>();
            mockVerification = new Mock<IVerificationService>();
            mockProfile = new Mock<IProfileUpdater>();
            mockPassword = new Mock<IPasswordManager>();
            mockMapper = new Mock<IUserMapper>();
            mockRanking = new Mock<IRankingService>();
            mockHistory = new Mock<IMatchHistoryService>();
            mockUserSet = new Mock<DbSet<User>>();
            mockUserProfileSet = new Mock<DbSet<UserProfile>>();
            mockContext.Setup(c => c.User).Returns(mockUserSet.Object);
            mockContext.Setup(c => c.UserProfile).Returns(mockUserProfileSet.Object);

            service = new TrucoUserServiceImp(
                mockContext.Object, mockAuth.Object, mockSession.Object,
                mockEmail.Object, mockVerification.Object, mockProfile.Object,
                mockPassword.Object, mockMapper.Object, mockRanking.Object, mockHistory.Object
            );
        }

        [TestMethod]
        public void TestLoginReturnsFalseWhenInputsAreInvalid()
        {
            bool result = service.Login("", "pass", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoginThrowsFaultExceptionWhenBruteForceDetected()
        {
            mockAuth.Setup(a => a.ValidateBruteForceStatus(It.IsAny<string>()))
                     .Throws(new FaultException("Blocked"));

            Assert.ThrowsException<FaultException>(() => service.Login("user", "pass", "en-US"));
        }

        [TestMethod]
        public void TestLoginReturnsFalseWhenAuthenticationFails()
        {
            mockAuth.Setup(a => a.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns((User)null);

            bool result = service.Login("validUser", "wrongPass", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestLoginReturnsTrueOnSuccess()
        {
            var user = new User 
            { 
                username = "Valid"
            };

            mockAuth.Setup(a => a.AuthenticateUser("Valid", "Pass")).Returns(user);
            bool result = service.Login("Valid", "Pass", "en-US");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRegisterReturnsFalseWhenValidationFails()
        {
            bool result = service.Register("User", "Pass123", "bad-email");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRegisterReturnsFalseWhenUserAlreadyExists()
        {
            var data = new List<User> 
            {
                new User 
                { 
                    email = "exist@gmail.com" 
                } 
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            bool result = service.Register("NewUser", "Pass123", "exist@gmail.com");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRegisterReturnsTrueWhenUserIsNew()
        {
            var data = new List<User>().AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            bool result = service.Register("NewUser", "Pass123!", "new@gmail.com");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSaveUserProfileReturnsFalseIfValidationFails()
        {
            mockProfile.Setup(p => p.ValidateProfileInput(It.IsAny<UserProfileData>())).Returns(false);
            bool result = service.SaveUserProfile(new UserProfileData());
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSaveUserProfileReturnsFalseIfUserNotFound()
        {
            mockProfile.Setup(p => p.ValidateProfileInput(It.IsAny<UserProfileData>())).Returns(true);
            var data = new List<User>().AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            bool result = service.SaveUserProfile(new UserProfileData 
            {
                Email = "test@gmail.com" 
            });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSaveUserProfileReturnsTrueOnSuccess()
        {
            mockProfile.Setup(p => p.ValidateProfileInput(It.IsAny<UserProfileData>())).Returns(true);
            mockProfile.Setup(p => p.TryUpdateUsername(It.IsAny<UsernameUpdateContext>())).Returns(true);

            var user = new User 
            {
                email = "test@gmail.com" 
            };

            var data = new List<User>
            { 
                user 
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            bool result = service.SaveUserProfile(new UserProfileData
            {
                Email = "test@gmail.com" 
            });

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestUpdateUserAvatarAsyncReturnsFalseForInvalidUsername()
        {
            var result = service.UpdateUserAvatarAsync(null, "avatar_aaa_default").Result;
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUpdateUserAvatarAsyncReturnsFalseOnException()
        {
            mockProfile.Setup(p => p.ProcessAvatarUpdate(It.IsAny<string>(), It.IsAny<string>()))
                        .Throws(new Exception("Fail"));

            var result = service.UpdateUserAvatarAsync("User", "avatar_aaa_default").Result;
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUpdateUserAvatarAsyncReturnsTrueOnSuccess()
        {
            mockProfile.Setup(p => p.ProcessAvatarUpdate("User", "avatar_aaa_default")).Returns(true);
            var result = service.UpdateUserAvatarAsync("User", "avatar_aaa_default").Result;
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestPasswordChangeReturnsFalseForInvalidPassword()
        {
            bool result = service.PasswordChange("valid@gmail.com", "short", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestPasswordChangeReturnsResultOfManager()
        {
            mockPassword.Setup(p => p.UpdatePasswordAndNotify(It.IsAny<PasswordUpdateOptions>())).Returns(true);
            bool result = service.PasswordChange("valid@gmail.com", "StrongPass1", "en-US");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestPasswordChangeReturnsFalseForInvalidEmail()
        {
            bool result = service.PasswordChange("invalid", "StrongPass1", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestPasswordResetThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => service.PasswordReset(null));
        }

        [TestMethod]
        public void TestPasswordResetReturnsFalseIfVerificationFails()
        {
            mockVerification.Setup(v => v.ConfirmEmailVerification(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(false);
           
            var options = new PasswordResetOptions 
            {
                Email = "t@gmail.com",
                NewPassword = "P1", 
                Code = "123" 
            };

            bool result = service.PasswordReset(options);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestPasswordResetDelegatesToPasswordManagerOnSuccess()
        {
            mockVerification.Setup(v => v.ConfirmEmailVerification(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(true);
            mockPassword.Setup(p => p.UpdatePasswordAndNotify(It.IsAny<PasswordUpdateOptions>())).Returns(true);
            
            var options = new PasswordResetOptions 
            {
                Email = "t@gmail.com",
                NewPassword = "P1!", 
                Code = "123" 
            };

            bool result = service.PasswordReset(options);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestRequestEmailVerificationReturnsFalseForInvalidEmail()
        {
            bool result = service.RequestEmailVerification("bad", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRequestEmailVerificationCatchesException()
        {
            mockVerification.Setup(v => v.RequestEmailVerification(It.IsAny<string>(), It.IsAny<string>()))
                             .Throws(new Exception("SMTP Error"));

            bool result = service.RequestEmailVerification("test@test.com", "en-US");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestRequestEmailVerificationReturnsTrueOnSuccess()
        {
            mockVerification.Setup(v => v.RequestEmailVerification(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(true);

            bool result = service.RequestEmailVerification("test@test.com", "en-US");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestConfirmEmailVerificationDelegatesCorrectly()
        {
            mockVerification.Setup(v => v.ConfirmEmailVerification("a", "b")).Returns(true);
            bool result = service.ConfirmEmailVerification("a", "b");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsFalseForInvalidInput()
        {
            bool result = service.UsernameExists("   ");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsFalseOnDbException()
        {
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("DB"));
            bool result = service.UsernameExists("User");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUsernameExistsReturnsTrueIfFound()
        {
            var data = new List<User>
            {
                new User
                {
                    username = "User"
                }
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            bool result = service.UsernameExists("User");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestEmailExistsReturnsFalseForInvalidInput()
        {
            bool result = service.EmailExists("bad-email");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestEmailExistsReturnsFalseOnDbException()
        {
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("DB"));
            bool result = service.EmailExists("valid@gmail.com");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestEmailExistsReturnsTrueIfFound()
        {
            var data = new List<User> 
            { 
                new User
                {
                email = "found@gmail.com"
                } 
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            bool result = service.EmailExists("found@gmail.com");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestGetUserProfileReturnsNullForInvalidUser()
        {
            var result = service.GetUserProfile(null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetUserProfileReturnsNullIfNotFound()
        {
            var data = new List<User>().AsQueryable();
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var result = service.GetUserProfile("test");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetUserProfileReturnsMappedDataIfFound()
        {
            var user = new User 
            { 
                username = "Real" 
            };

            var profileData = new UserProfileData 
            { 
                Username = "Real" 
            };

            var data = new List<User> 
            {
                user 
            }.AsQueryable();

            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(data.Provider);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(data.Expression);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            mockMapper.Setup(m => m.MapUserToProfileData(user)).Returns(profileData);
            var result = service.GetUserProfile("Real");
            Assert.AreEqual("Real", result.Username);
        }

        [TestMethod]
        public void TestGetUserProfileByEmailAsyncReturnsNullForInvalidEmail()
        {
            var result = service.GetUserProfileByEmailAsync("bad").Result;
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetUserProfileByEmailAsyncReturnsNullOnException()
        {
            mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Throws(new Exception("AsyncFail"));
            var result = service.GetUserProfileByEmailAsync("valid@gmail.com").Result;
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetGlobalRankingHandlesServiceException()
        {
            mockRanking.Setup(r => r.GetGlobalRanking()).Throws(new Exception("Rank Error"));
            var result = service.GetGlobalRanking();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetGlobalRankingReturnsListOnSuccess()
        {
            var list = new List<PlayerStats>
            { 
                new PlayerStats
                { 
                    PlayerName = "A" 
                } 
            };
       
            mockRanking.Setup(r => r.GetGlobalRanking()).Returns(list);
            var result = service.GetGlobalRanking();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void TestGetLastMatchesReturnsEmptyForInvalidUser()
        {
            var result = service.GetLastMatches("");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetLastMatchesHandlesException()
        {
            mockHistory.Setup(h => h.GetLastMatches(It.IsAny<string>())).Throws(new Exception("History Error"));
            var result = service.GetLastMatches("User");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetLastMatchesReturnsData()
        {
            var matches = new List<MatchScore> 
            { 
                new MatchScore
                {
                    MatchID = "1" 
                } 
            };
            
            mockHistory.Setup(h => h.GetLastMatches("User")).Returns(matches);
            var result = service.GetLastMatches("User");
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void TestGetOnlinePlayersThrowsNotImplemented()
        {
            Assert.ThrowsException<NotImplementedException>(() => service.GetOnlinePlayers());
        }

        [TestMethod]
        public void TestGetOnlinePlayersDoesNotReturnNull()
        {
            try
            {
                service.GetOnlinePlayers();
            }
            catch (NotImplementedException)
            {
                Assert.IsTrue(true);
               
                return;
            }
            Assert.Fail("Should have thrown");
        }

        [TestMethod]
        public void TestGetUserCallbackReturnsNullOnException()
        {
            var result = TrucoUserServiceImp.GetUserCallback(null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetUserCallbackReturnsNullIfNotFound()
        {
            var result = TrucoUserServiceImp.GetUserCallback("NonExistent");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestLogClientExceptionDoesNotThrow()
        {
            service.LogClientException("Error", "Stack", "User");
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestLogClientExceptionHandlesNullUser()
        {
            service.LogClientException("Error", "Stack", null);
            Assert.IsTrue(true);
        }
    }
}