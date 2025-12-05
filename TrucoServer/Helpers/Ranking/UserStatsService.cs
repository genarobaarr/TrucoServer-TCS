using System.Linq;

namespace TrucoServer.Helpers.Ranking
{
    public class UserStatsService : IUserStatsService
    {
        public void UpdateUserStats(baseDatosTrucoEntities context, int userId, bool isWinner)
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