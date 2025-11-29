using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer.Tests.HelpersTests.RankingTests
{
    [TestClass]
    public class RankingServiceTests
    {
        private Helpers.Ranking.RankingService rankingService;

        [TestInitialize]
        public void Setup()
        {
            rankingService = new Helpers.Ranking.RankingService();
            using (var context = new baseDatosTrucoEntities())
            {
                if (!context.User.Any(u => u.username == "WinnerUser"))
                {
                    context.User.Add(new User  
                    { 
                        username = "WinnerUser",
                        wins = 100, 
                        email = "w@gmail.com",
                        passwordHash = "x" 
                    });

                    context.User.Add(new User 
                    { 
                        username = "LoserUser",
                        wins = 0,
                        email = "l@gmail.com", 
                        passwordHash = "x" 
                    });

                    context.SaveChanges();
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new baseDatosTrucoEntities())
            {
                var u1 = context.User.FirstOrDefault(u => u.username == "WinnerUser");
                var u2 = context.User.FirstOrDefault(u => u.username == "LoserUser");
                if (u1 != null) context.User.Remove(u1);
                if (u2 != null) context.User.Remove(u2);
                context.SaveChanges();
            }
        }

        [TestMethod]
        public void TestGetGlobalRankingShouldReturnOrderedList()
        {
            var result = rankingService.GetGlobalRanking();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count >= 2);

            var winner = result.FirstOrDefault(p => p.PlayerName == "WinnerUser");
            var loser = result.FirstOrDefault(p => p.PlayerName == "LoserUser");

            Assert.IsNotNull(winner);
            Assert.IsNotNull(loser);
            Assert.IsTrue(winner.Wins > loser.Wins);
        }
    }
}
