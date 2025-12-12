using System;
using System.Collections.Generic;
using TrucoServer.Data.DTOs;
using System.Linq;

namespace TrucoServer.Helpers.Match
{
    public class GamePlayerBuilder
    {
        private const string GUEST_PREFIX = "Guest_";
        private const string TEAM_1 = "Team 1";

        private readonly ILobbyCoordinator coordinator;
        private readonly baseDatosTrucoEntities context;

        public GamePlayerBuilder(baseDatosTrucoEntities context, ILobbyCoordinator coordinator)
        {
            this.context = context;
            this.coordinator = coordinator;
        }

        public MatchPlayersResult BuildParticipants(List<PlayerInformation> playersList)
        {
            var result = new MatchPlayersResult();

            foreach (var pInfo in playersList)
            {
                if (pInfo.Username.StartsWith(GUEST_PREFIX))
                {
                    AddGuestPlayer(pInfo, result);
                }
                else
                {
                    AddRegisteredPlayer(pInfo, result);
                }
            }
            return result;
        }

        private void AddGuestPlayer(PlayerInformation info, MatchPlayersResult result)
        {
            if (coordinator.TryGetActiveCallbackForPlayer(info.Username, out var callback))
            {
                int guestId = (int)-Math.Abs((long)info.Username.GetHashCode());
                var registeredInfo = coordinator.GetPlayerInfoFromCallback(callback);
                string team = registeredInfo?.Team ?? info.Team ?? TEAM_1;

                result.Players.Add(new PlayerInformationWithConstructor(guestId, info.Username, team));
                result.Callbacks[guestId] = callback;
            }
        }

        private void AddRegisteredPlayer(PlayerInformation info, MatchPlayersResult result)
        {
            var user = context.User.FirstOrDefault(u => u.username == info.Username);
           
            if (user != null && coordinator.TryGetActiveCallbackForPlayer(info.Username, out var callback))
            {
                result.Players.Add(new PlayerInformationWithConstructor(user.userID, user.username, info.Team));
                result.Callbacks[user.userID] = callback;
            }
        }
    }
}