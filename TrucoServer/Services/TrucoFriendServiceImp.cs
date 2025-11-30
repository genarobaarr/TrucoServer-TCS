using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;
using TrucoServer.Helpers.Friends;

namespace TrucoServer.Services
{
    public class TrucoFriendServiceImp : ITrucoFriendService
    {
        private const string STATUS_ACCEPTED = "Accepted";
        private const string STATUS_PENDING = "Pending";

        private readonly IFriendRepository friendshipRepository;
        private readonly IFriendNotifier notificationService;

        public TrucoFriendServiceImp()
        {
            this.friendshipRepository = new FriendRepository();
            this.notificationService = new FriendNotifier();
        }

        public bool SendFriendRequest(string fromUser, string toUser)
        {
            if (!ServerValidator.IsUsernameValid(fromUser) ||
                !ServerValidator.IsUsernameValid(toUser) ||
                fromUser.Equals(toUser, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (!friendshipRepository.GetUsersFromDatabase(context, fromUser, toUser, out User requester, out User target))
                    {
                        return false;
                    }

                    if (friendshipRepository.CheckFriendshipExists(context, requester.userID, target.userID))
                    {
                        return false;
                    }

                    friendshipRepository.RegisterFriendRequest(context, requester.userID, target.userID, STATUS_PENDING);
                    notificationService.NotifyRequestReceived(toUser, fromUser);

                    return true;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SendFriendRequest));
                return false;
            }
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            if (!ServerValidator.IsUsernameValid(fromUser) || !ServerValidator.IsUsernameValid(toUser))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (!friendshipRepository.GetUsersFromDatabase(context, fromUser, toUser, out User requester, out User acceptor))
                    {
                        return false;
                    }

                    var request = friendshipRepository.FindPendingFriendship(context, requester.userID, acceptor.userID, STATUS_PENDING);
                    
                    if (request == null)
                    {
                        return false;
                    }

                    friendshipRepository.CommitFriendshipAcceptance(context, request, requester.userID, acceptor.userID, STATUS_ACCEPTED);
                    notificationService.NotifyRequestAccepted(fromUser, toUser);

                    return true;
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
            }
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            if (!ServerValidator.IsUsernameValid(user1) || !ServerValidator.IsUsernameValid(user2))
            {
                return false;
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    if (!friendshipRepository.GetUsersFromDatabase(context, user1, user2, out User u1, out User u2))
                    {
                        return false;
                    }

                    return friendshipRepository.DeleteFriendships(context, u1.userID, u2.userID);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(RemoveFriendOrRequest));
                return false;
            }
        }

        public List<FriendData> GetFriends(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return new List<FriendData>();
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username == username);
                   
                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    return friendshipRepository.QueryFriendsList(context, user.userID, STATUS_ACCEPTED);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetFriends));
                return new List<FriendData>();
            }
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            if (!ServerValidator.IsUsernameValid(username))
            {
                return new List<FriendData>();
            }

            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    var user = context.User.SingleOrDefault(u => u.username == username);

                    if (user == null)
                    {
                        return new List<FriendData>();
                    }

                    return friendshipRepository.QueryPendingRequests(context, user.userID, STATUS_PENDING);
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetPendingFriendRequests));
                return new List<FriendData>();
            }
        }
    }
}
