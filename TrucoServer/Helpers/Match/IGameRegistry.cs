namespace TrucoServer.Helpers.Match
{
    public interface IGameRegistry
    {
        bool TryAddGame(string matchCode, GameLogic.TrucoMatch match);
        bool TryGetGame(string matchCode, out GameLogic.TrucoMatch match);
        bool TryRemoveGame(string matchCode);
        void AbortAndRemoveGame(string matchCode, string player);
    }
}
