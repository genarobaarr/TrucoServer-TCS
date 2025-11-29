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
        private const string TEST_USER = "HistoryUser";

        [TestInitialize]
        public void Setup()
        {
            historyService = new Helpers.Ranking.MatchHistoryService();
            using (var context = new baseDatosTrucoEntities())
            {
                var user = new User { username = TEST_USER, email = "h@gmail.com", passwordHash = "password" };
                context.User.Add(user);
                context.SaveChanges();

                var match = new Match { lobbyID = 1, status = "Finished", endedAt = DateTime.Now, versionID = 1 };
                context.Match.Add(match);
                context.SaveChanges();

                var mp = new MatchPlayer { matchID = match.matchID, userID = user.userID, team = "Team 1", score = 30, isWinner = true };
                context.MatchPlayer.Add(mp);
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
                    var mps = context.MatchPlayer.Where(mp => mp.userID == user.userID);
                    context.MatchPlayer.RemoveRange(mps);
                    context.User.Remove(user);
                    context.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void TestGetLastMatchesExistingUserShouldReturnMatches()
        {
            var result = historyService.GetLastMatches(TEST_USER);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result[0].IsWin);
        }

        [TestMethod]
        public void TestGetLastMatchesInvalidUsernameShouldReturnEmpty()
        {
            var result = historyService.GetLastMatches("");
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
