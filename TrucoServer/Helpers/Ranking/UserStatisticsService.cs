using System;
using System.Linq;

namespace TrucoServer.Helpers.Ranking
{
    public class UserStatisticsService : IUserStatisticsService
    {
        private readonly baseDatosTrucoEntities context;

        public UserStatisticsService(baseDatosTrucoEntities context)
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