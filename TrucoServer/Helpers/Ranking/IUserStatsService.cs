namespace TrucoServer.Helpers.Ranking
{
    public interface IUserStatsService
    {
        void UpdateUserStats(baseDatosTrucoEntities context, int userId, bool isWinner);
    }
}
