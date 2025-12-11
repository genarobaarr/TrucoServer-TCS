using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests.HelpersTests.RankingTests
{
    [TestClass]
    public class MatchHistoryServiceTests
    {
        private Helpers.Ranking.MatchHistoryService historyService;

        private const int LOBBY_ID = 5;
        private const int VERSION_ID = 1;
        private const int SCORE = 30;

        [TestInitialize]
        public void Setup()
        {
            historyService = new Helpers.Ranking.MatchHistoryService();
            using (var context = new baseDatosTrucoEntities())
            {
                var user = new User 
                { 
                    username = "NoobMaster69", 
                    email = "h@gmail.com", 
                    passwordHash = "password" 
                };

                context.User.Add(user);
                context.SaveChanges();

                var match = new Match 
                {
                    lobbyID = LOBBY_ID, 
                    status = "Finished",
                    startedAt = DateTime.Now.AddMinutes(-30),
                    endedAt = DateTime.Now, 
                    versionID = VERSION_ID
                };

                context.Match.Add(match);
                context.SaveChanges();

                var mp = new MatchPlayer 
                { 
                    matchID = match.matchID, 
                    userID = user.userID,
                    team = "Team 1",
                    score = SCORE,
                    isWinner = true 
                };

                context.MatchPlayer.Add(mp);
                context.SaveChanges();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var user = context.User.FirstOrDefault(u => u.username == "NoobMaster69");
                
                if (user != null)
                {
                    var mps = context.MatchPlayer.Where(mp => mp.userID == user.userID);
                    context.MatchPlayer.RemoveRange(mps);
                    context.User.Remove(user);
                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestGetLastMatchesExistingUserShouldReturnNonEmptyList()
        {
            var result = historyService.GetLastMatches("NoobMaster69");
            Assert.IsTrue(result?.Count > 0);
        }

        [TestMethod]
        public void TestGetLastMatchesMostRecentMatchShouldBeWin()
        {
            var result = historyService.GetLastMatches("NoobMaster69");
            Assert.IsTrue(result?[0].IsWin ?? false);
        }

        [TestMethod]
        public void TestGetLastMatchesInvalidUsernameShouldReturnEmpty()
        {
            var result = historyService.GetLastMatches(string.Empty);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestGetLastMatchesNonExistentUserShouldReturnEmpty()
        {
            var result = historyService.GetLastMatches("TestUser");
            Assert.AreEqual(0, result.Count);
        }
    }
}
