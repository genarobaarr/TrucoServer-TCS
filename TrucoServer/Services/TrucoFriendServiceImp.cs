using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Friends;
using TrucoServer.Langs;
using TrucoServer.Utilities;

namespace TrucoServer.Services
{
    public class TrucoFriendServiceImp : ITrucoFriendService
    {
        private const string ERROR_CODE_DB_ERROR_GET_FRIENDS = "ServerDBErrorGetFriends";
        private const string ERROR_CODE_DB_ERROR_FRIEND_REQUEST = "ServerDBErrorFriendRequest";
        private const string ERROR_CODE_FRIEND_REQUEST_NOT_FOUND = "FriendRequestUserNotFound";
        private const string ERROR_CODE_FRIEND_ALREADY_EXISTS = "FriendRequestAlreadyFriends";
        private const string ERROR_CODE_GENERAL_ERROR = "ServerError";
        private const string ERROR_CODE_TIMEOUT_ERROR = "ServerTimeout";
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

        public TrucoFriendServiceImp(baseDatosTrucoEntities context, IFriendRepository friendshipRepository, IFriendNotifier notificationService)
        {
            this.context = context;
            this.friendshipRepository = friendshipRepository;
            this.notificationService = notificationService;
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
                    throw FaultFactory.CreateFault(ERROR_CODE_FRIEND_REQUEST_NOT_FOUND, string.Format(Lang.ExceptionTextFriendRequestUserNotFound, toUser));
                }

                User requester = lookupResult.User1;
                User target = lookupResult.User2;

                if (friendshipRepository.CheckFriendshipExists(requester.userID, target.userID))
                {
                    throw FaultFactory.CreateFault(ERROR_CODE_FRIEND_ALREADY_EXISTS, string.Format(Lang.ExceptionTextFriendRequestAlreadyFriends, toUser));
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
            catch (FaultException<CustomFault>)
            {
                throw;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(SendFriendRequest));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_FRIEND_REQUEST, Lang.ExceptionTextDBErrorFriendRequest);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(SendFriendRequest));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_FRIEND_REQUEST, Lang.ExceptionTextDBErrorFriendRequest);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(SendFriendRequest));
                throw FaultFactory.CreateFault(ERROR_CODE_TIMEOUT_ERROR, Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(SendFriendRequest));
                throw FaultFactory.CreateFault(ERROR_CODE_GENERAL_ERROR, Lang.ExceptionTextErrorOcurred);
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return true;
            }
            catch (CommunicationException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
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
            catch (DbUpdateException ex)
            {
                ServerException.HandleException(ex, nameof(RemoveFriendOrRequest));
                return false;
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(AcceptFriendRequest));
                return false;
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetFriends));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_GET_FRIENDS, Lang.ExceptionTextDBErrorGetFriends);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetFriends));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_GET_FRIENDS, Lang.ExceptionTextDBErrorGetFriends);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetFriends));
                throw FaultFactory.CreateFault(ERROR_CODE_TIMEOUT_ERROR, Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetFriends));
                throw FaultFactory.CreateFault(ERROR_CODE_GENERAL_ERROR, Lang.ExceptionTextErrorOcurred);
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetPendingFriendRequests));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_GET_FRIENDS, Lang.ExceptionTextDBErrorGetFriends);
            }
            catch (EntityException ex)
            {
                ServerException.HandleException(ex, nameof(GetPendingFriendRequests));
                throw FaultFactory.CreateFault(ERROR_CODE_DB_ERROR_GET_FRIENDS, Lang.ExceptionTextDBErrorGetFriends);
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetPendingFriendRequests));
                throw FaultFactory.CreateFault(ERROR_CODE_TIMEOUT_ERROR, Lang.ExceptionTextTimeout);
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetPendingFriendRequests));
                throw FaultFactory.CreateFault(ERROR_CODE_GENERAL_ERROR, Lang.ExceptionTextErrorOcurred);
            }
        }
    }
}
