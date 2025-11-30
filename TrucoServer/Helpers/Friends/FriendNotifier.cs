using System;
using TrucoServer.Utilities;
using TrucoServer.Services;

namespace TrucoServer.Helpers.Friends
{
    public class FriendNotifier : IFriendNotifier
    {
        public void NotifyRequestReceived(string targetUsername, string fromUsername)
        {
            try
            {
                var callback = TrucoUserServiceImp.GetUserCallback(targetUsername);

                if (callback != null)
                {
                    callback.OnFriendRequestReceived(fromUsername);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestReceived));
            }
        }

        public void NotifyRequestAccepted(string targetUsername, string fromUsername)
        {
            try
            {
                var callback = TrucoUserServiceImp.GetUserCallback(targetUsername);

                if (callback != null)
                {
                    callback.OnFriendRequestAccepted(fromUsername);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestAccepted));
            }
        }
    }
}
