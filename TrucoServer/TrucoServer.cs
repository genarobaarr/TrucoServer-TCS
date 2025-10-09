using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using TrucoServer;

namespace TrucoServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public partial class TrucoServer : ITrucoUserService
    {
        bool ITrucoUserService.Register(string username, string password, string email)
        {

            return true;
        }
        bool ITrucoUserService.Login(string username, string password)
        {
            throw new NotImplementedException();
        }
        void ITrucoUserService.Logout(string username)
        {
            throw new NotImplementedException();
        }
        List<PlayerStats> ITrucoUserService.GetGlobalRanking()
        {
            throw new NotImplementedException();
        }
        List<MatchResult> ITrucoUserService.GetLastMatches(string username)
        {
            throw new NotImplementedException();
        }

        List<string> ITrucoUserService.GetOnlinePlayers()
        {
            throw new NotImplementedException();
        }
    }

    public partial class TrucoServer : ITrucoFriendService
    {
        bool ITrucoFriendService.SendFriendRequest(string fromUser, string toUser)
        {
            throw new NotImplementedException();
        }

        void ITrucoFriendService.AcceptFriendRequest(string fromUser, string toUser)
        {
            throw new NotImplementedException();
        }
        List<string> ITrucoFriendService.GetFriends(string username)
        {
            throw new NotImplementedException();
        }
    }

    public partial class TrucoServer : ITrucoMatchService
    {
        string ITrucoMatchService.CreateMatch(string hostPlayer)
        {
            throw new NotImplementedException();
        }

        bool ITrucoMatchService.JoinMatch(string matchCode, string player)
        {
            throw new NotImplementedException();
        }

        void ITrucoMatchService.LeaveMatch(string matchCode, string player)
        {
            throw new NotImplementedException();
        }
        void ITrucoMatchService.PlayCard(string matchCode, string player, string card)
        {
            throw new NotImplementedException();
        }

        void ITrucoMatchService.SendChatMessage(string matchCode, string player, string message)
        {
            throw new NotImplementedException();
        }
    }
}