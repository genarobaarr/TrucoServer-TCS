using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly baseDatosTrucoEntities context;

        public TrucoFriendServiceImp()
        {
            this.context = new baseDatosTrucoEntities();
            this.friendshipRepository = new FriendRepository(context);
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
                var lookupOptions = new UserLookupOptions { Username1 = fromUser, Username2 = toUser };
                var lookupResult = friendshipRepository.GetUsersFromDatabase(lookupOptions);

                if (!lookupResult.Success)
                {
                    return false;
                }

                User requester = lookupResult.User1;
                User target = lookupResult.User2;

                if (friendshipRepository.CheckFriendshipExists(requester.userID, target.userID))
                {
                    return false;
                }

                var requestDto = new FriendRequest
                {
                    RequesterId = requester.userID,
                    TargetId = target.userID,
                    Status = STATUS_PENDING
                };

                friendshipRepository.RegisterFriendRequest(requestDto);
                notificationService.NotifyRequestReceived(toUser, fromUser);

                return true;
                
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
                var lookupOptions = new UserLookupOptions { Username1 = fromUser, Username2 = toUser };
                var lookupResult = friendshipRepository.GetUsersFromDatabase(lookupOptions);

                if (!lookupResult.Success)
                {
                    return false;
                }

                User requester = lookupResult.User1;
                User target = lookupResult.User2;

                var searchCriteria = new FriendRequest
                {
                    RequesterId = requester.userID,
                    TargetId = target.userID,
                    Status = STATUS_PENDING
                };

                var request = friendshipRepository.FindPendingFriendship(searchCriteria);

                if (request == null)
                {
                    return false;
                }

                var commitOptions = new FriendshipCommitOptions
                {
                    Request = request,
                    RequesterId = requester.userID,
                    AcceptorId = target.userID,
                    StatusAccepted = STATUS_ACCEPTED
                };

                friendshipRepository.CommitFriendshipAcceptance(commitOptions);
                notificationService.NotifyRequestAccepted(fromUser, toUser);

                return true;
                
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
                var lookupOptions = new UserLookupOptions { Username1 = user1, Username2 = user2 };
                var lookupResult = friendshipRepository.GetUsersFromDatabase(lookupOptions);

                if (!lookupResult.Success)
                {
                    return false;
                }

                User requester = lookupResult.User1;
                User target = lookupResult.User2;

                return friendshipRepository.DeleteFriendships(requester.userID, target.userID);
                
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
                var user = context.User.SingleOrDefault(u => u.username == username);
                   
                if (user == null)
                {
                    return new List<FriendData>();
                }

                return friendshipRepository.QueryFriendsList(user.userID, STATUS_ACCEPTED);
                
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
                var user = context.User.SingleOrDefault(u => u.username == username);

                if (user == null)
                {
                    return new List<FriendData>();
                }

                return friendshipRepository.QueryPendingRequests(user.userID, STATUS_PENDING);
                
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetPendingFriendRequests));
                return new List<FriendData>();
            }
        }
    }
}
