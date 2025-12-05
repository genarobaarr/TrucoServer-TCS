namespace TrucoServer.Helpers.Ranking
{
    public interface IUserStatsService
    {
        void UpdateUserStats(int userId, bool isWinner);
    }
}
