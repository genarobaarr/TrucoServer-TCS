using System;
using System.ServiceModel;
using TrucoServer.Services;
using TrucoServer.Utilities;

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
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestReceived));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestReceived));
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
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestAccepted));
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestAccepted));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(NotifyRequestAccepted));
            }
        }
    }
}
