namespace TrucoServer.Helpers.Ranking
{
    public interface IUserStatisticsService
    {
        void UpdateUserStats(int userId, bool isWinner);
    }
}
