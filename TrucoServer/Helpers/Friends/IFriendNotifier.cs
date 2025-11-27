namespace TrucoServer.Helpers.Friends
{
    public interface IFriendNotifier
    {
        void NotifyRequestReceived(string targetUsername, string fromUsername);
        void NotifyRequestAccepted(string targetUsername, string fromUsername);
    }
}
