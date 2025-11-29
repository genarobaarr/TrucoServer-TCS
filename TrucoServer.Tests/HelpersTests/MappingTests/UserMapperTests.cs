using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrucoServer.Helpers.Mapping;

namespace TrucoServer.Tests.HelpersTests.MappingTests
{
    [TestClass]
    public class UserMapperTests
    {
        private UserMapper mapper;

        [TestInitialize]
        public void Setup()
        {
            mapper = new UserMapper();
        }

        [TestMethod]
        public void TestMapUserToProfileDataValidUserShouldReturnDtoName()
        {
            var user = new User
            {
                username = "NameTest",
                email = "test@gmail.com",
                UserProfile = new UserProfile 
                { 
                    avatarID = "avatar_aaa_default", 
                    languageCode = "es-MX" 
                }
            };

            var result = mapper.MapUserToProfileData(user);

            Assert.AreEqual("NameTest", result.Username);
        }

        [TestMethod]
        public void TestMapUserToProfileDataValidUserShouldReturnDtoAvatar()
        {
            var user = new User
            {
                username = "NameTest",
                email = "test@gmail.com",
                UserProfile = new UserProfile
                {
                    avatarID = "avatar_aaa_default",
                    languageCode = "es-MX"
                }
            };

            var result = mapper.MapUserToProfileData(user);

            Assert.AreEqual("avatar_aaa_default", result.AvatarId);
        }

        [TestMethod]
        public void TestFetchLastMatchesForUserFinishedMatchesShouldReturnList()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = new User 
                { 
                    username = "MatchUser",
                    email = "m@gmail.com", 
                    passwordHash = "Xx_LaMaquina_xX", 
                    nameChangeCount = 0 
                };

                context.User.Add(user);
                context.SaveChanges();

                var match = new Match 
                { 
                    lobbyID = 1,
                    status = "Finished",
                    endedAt = DateTime.Now, 
                    versionID = 1 
                };

                context.Match.Add(match);
                context.SaveChanges();

                var mp = new MatchPlayer 
                {
                    matchID = match.matchID,
                    userID = user.userID, 
                    team = "Team 1", score = 30, 
                    isWinner = true 
                };

                context.MatchPlayer.Add(mp);
                context.SaveChanges();

                var results = mapper.FetchLastMatchesForUser(context, user.userID);

                Assert.IsTrue(results.Count > 0);
                Assert.AreEqual(match.matchID.ToString(), results[0].MatchID);
                Assert.AreEqual(30, results[0].FinalScore);

                context.MatchPlayer.Remove(mp);
                context.Match.Remove(match);
                context.User.Remove(user);
                context.SaveChanges();
            }
        }
    }
}
