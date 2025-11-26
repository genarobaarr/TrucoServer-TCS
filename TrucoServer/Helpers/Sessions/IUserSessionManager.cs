namespace TrucoServer.Helpers.Sessions
{
    public interface IUserSessionManager
    {
        void RegisterSession(string realUsername);
        Contracts.ITrucoCallback GetUserCallback(string username);
        void HandleExistingSession(string realUsername);
        bool IsCallbackActive(Contracts.ITrucoCallback callback);
    }
}
