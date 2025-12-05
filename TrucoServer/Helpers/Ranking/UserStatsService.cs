using System;
using System.Linq;

namespace TrucoServer.Helpers.Ranking
{
    public class UserStatsService : IUserStatsService
    {
        private readonly baseDatosTrucoEntities context;

        public UserStatsService(baseDatosTrucoEntities context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void UpdateUserStats(int userId, bool isWinner)
        {
            var user = context.User.FirstOrDefault(u => u.userID == userId);
            if (user != null)
            {
                if (isWinner)
                {
                    user.wins++;
                }
                else
                {
                    user.losses++;
                }
            }
        }
    }
}