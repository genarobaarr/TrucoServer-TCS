using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class TrucoServer : ITrucoUserService, ITrucoFriendService, ITrucoMatchService,
ITrucoTournamentService
    {
        private readonly ITrucoUserService userService;
        private readonly ITrucoFriendService friendService;
        private readonly ITrucoMatchService matchService;
        private readonly ITrucoTournamentService tournamentService;

        public TrucoServer()
        {
            userService = new TrucoUserServiceImplementation();
            friendService = new TrucoFriendServiceImplementation();
            matchService = new TrucoMatchServiceImplementation();
            tournamentService = new TrucoTournamentServiceImplementation();
        }



        public bool Login(string username, string password, string languageCode)
        {
            return userService.Login(username, password, languageCode);
        }

        public bool Register(string username, string password, string email)
        {
            return userService.Register(username, password, email);
        }

        public bool UsernameExists(string username)
        {
            return userService.UsernameExists(username);
        }

        public bool EmailExists(string email)
        {
            return userService.EmailExists(email);
        }

        public UserProfileData GetUserProfile(string username)
        {
            return userService.GetUserProfile(username);
        }

        public UserProfileData GetUserProfileByEmailAsync(string email)
        {
            return userService.GetUserProfileByEmailAsync(email);
        }

        public bool SaveUserProfile(UserProfileData profile)
        {
            return userService.SaveUserProfile(profile);
        }

        public bool UpdateUserAvatarAsync(string username, string newAvatarId)
        {
            return userService.UpdateUserAvatarAsync(username, newAvatarId);
        }

        public bool PasswordChange(string email, string newPassword, string languageCode)
        {
            return userService.PasswordChange(email, newPassword, languageCode);
        }

        public bool PasswordReset(PasswordResetOptions options)
        {
            return userService.PasswordReset(options);
        }

        public bool RequestEmailVerification(string email, string languageCode)
        {
            return userService.RequestEmailVerification(email, languageCode);
        }

        public bool ConfirmEmailVerification(string email, string code)
        {
            return userService.ConfirmEmailVerification(email, code);
        }

        public List<PlayerStatistics> GetGlobalRanking()
        {
            return userService.GetGlobalRanking();
        }

        public List<MatchScore> GetLastMatches(string username)
        {
            return userService.GetLastMatches(username);
        }

        public void LogClientException(string errorMessage, string stackTrace, string clientUsername)
        {
            userService.LogClientException(errorMessage, stackTrace, clientUsername);
        }



        public bool SendFriendRequest(string fromUser, string toUser)
        {
            return friendService.SendFriendRequest(fromUser, toUser);
        }

        public bool AcceptFriendRequest(string fromUser, string toUser)
        {
            return friendService.AcceptFriendRequest(fromUser, toUser);
        }

        public bool RemoveFriendOrRequest(string user1, string user2)
        {
            return friendService.RemoveFriendOrRequest(user1, user2);
        }

        public List<FriendData> GetFriends(string username)
        {
            return friendService.GetFriends(username);
        }

        public List<FriendData> GetPendingFriendRequests(string username)
        {
            return friendService.GetPendingFriendRequests(username);
        }



        public string CreateLobby(string hostUsername, int maxPlayers, string privacy)
        {
            return matchService.CreateLobby(hostUsername, maxPlayers, privacy);
        }

        public int JoinMatch(string matchCode, string player)
        {
            return matchService.JoinMatch(matchCode, player);
        }

        public void LeaveMatch(string matchCode, string player)
        {
            matchService.LeaveMatch(matchCode, player);
        }

        public void StartMatch(string matchCode)
        {
            matchService.StartMatch(matchCode);
        }

        public List<PublicLobbyInformation> GetPublicLobbies()
        {
            return matchService.GetPublicLobbies();
        }

        public void JoinMatchChat(string matchCode, string player)
        {
            matchService.JoinMatchChat(matchCode, player);
        }

        public void LeaveMatchChat(string matchCode, string player)
        {
            matchService.LeaveMatchChat(matchCode, player);
        }

        public void SendChatMessage(string matchCode, string player, string message)
        {
            matchService.SendChatMessage(matchCode, player, message);
        }

        public void PlayCard(string matchCode, string cardFileName)
        {
            matchService.PlayCard(matchCode, cardFileName);
        }

        public void CallTruco(string matchCode, string betType)
        {
            matchService.CallTruco(matchCode, betType);
        }

        public void RespondToCall(string matchCode, string response)
        {
            matchService.RespondToCall(matchCode, response);
        }

        public void CallEnvido(string matchCode, string betType)
        {
            matchService.CallEnvido(matchCode, betType);
        }

        public void RespondToEnvido(string matchCode, string response)
        {
            matchService.RespondToEnvido(matchCode, response);
        }

        public void CallFlor(string matchCode, string betType)
        {
            matchService.CallFlor(matchCode, betType);
        }

        public void RespondToFlor(string matchCode, string response)
        {
            matchService.RespondToFlor(matchCode, response);
        }

        public void GoToDeck(string matchCode)
        {
            matchService.GoToDeck(matchCode);
        }

        public void SwitchTeam(string matchCode, string username)
        {
            matchService.SwitchTeam(matchCode, username);
        }

        public bool InviteFriend(InviteFriendOptions inviteContext)
        {
            return matchService.InviteFriend(inviteContext);
        }

        public List<PlayerInformation> GetLobbyPlayers(string matchCode)
        {
            return matchService.GetLobbyPlayers(matchCode);
        }

        public BannedWordList GetBannedWords()
        {
            return matchService.GetBannedWords();
        }



        public string CreateTournament(int capacity, int hostUserId)
        {
            return tournamentService.CreateTournament(capacity, hostUserId);
        }

        public bool JoinTournamentByCode(string code, int userId)
        {
            return tournamentService.JoinTournamentByCode(code, userId);
        }

        public bool StartTournament(string code, int hostUserId)
        {
            return tournamentService.StartTournament(code, hostUserId);
        }

        public bool LeaveTournament(string code, int userId)
        {
            return tournamentService.LeaveTournament(code, userId);
        }

        public List<BracketDTO> GetTournamentTree(int tournamentId)
        {
            return tournamentService.GetTournamentTree(tournamentId);
        }

        public void ReportMatchResult(string tournamentCode, string matchCode, int winnerUserId)
        {
            tournamentService.ReportMatchResult(tournamentCode, matchCode, winnerUserId);
        }

        public List<TournamentDTO> GetAvailableTournaments()
        {
            return tournamentService.GetAvailableTournaments();
        }
    }
}