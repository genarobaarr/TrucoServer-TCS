using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Utilities;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Sessions
{
    public class UserSessionManager : IUserSessionManager
    {
        private static readonly ConcurrentDictionary<string, ITrucoCallback> onlineUsers = new ConcurrentDictionary<string, ITrucoCallback>();

        public void RegisterSession(string realUsername)
        {
            ITrucoCallback currentCallback = OperationContext.Current.GetCallbackChannel<ITrucoCallback>();
            onlineUsers.AddOrUpdate(realUsername, currentCallback, (key, oldValue) => currentCallback);
        }

        public ITrucoCallback GetUserCallback(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            try
            {
                if (onlineUsers.TryGetValue(username, out ITrucoCallback callback))
                {
                    var communicationObject = callback as ICommunicationObject;

                    if (communicationObject == null)
                    {
                        onlineUsers.TryRemove(username, out _);
                        return null;
                    }

                    if (communicationObject.State == CommunicationState.Opened)
                    {
                        return callback;
                    }

                    try
                    {
                        communicationObject.Abort();
                    }
                    catch
                    {
                        /* Intentionally ignoring exceptions during cleanup. 
                         * If Abort() fails, we still need to remove the 
                         * invalid user from the dictionary without 
                         * interrupting the flow.
                         */
                    }

                    onlineUsers.TryRemove(username, out _);
                }
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(GetUserCallback));
            }
            catch (ObjectDisposedException ex)
            {
                onlineUsers.TryRemove(username, out _);
                ServerException.HandleException(ex, nameof(GetUserCallback));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetUserCallback));
            }

            return null;
        }

        public void HandleExistingSession(string realUsername)
        {
            if (!onlineUsers.ContainsKey(realUsername))
            {
                return;
            }

            var oldCallback = onlineUsers[realUsername];
            bool isZombie = !IsCallbackActive(oldCallback);

            if (!isZombie)
            {
                var fault = new LoginFault
                {
                    ErrorCode = "UserAlreadyLoggedIn",
                    ErrorMessage = Langs.Lang.ExceptionTextLogin
                };

                throw new FaultException<LoginFault>(fault, new FaultReason("UserAlreadyLoggedIn"));
            }
            else
            {
                onlineUsers.TryRemove(realUsername, out _);
            }
        }

        public bool IsCallbackActive(ITrucoCallback callback)
        {
            if (callback is ICommunicationObject channel && channel.State == CommunicationState.Opened)
            {
                try
                {
                    callback.Ping();
                   
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
