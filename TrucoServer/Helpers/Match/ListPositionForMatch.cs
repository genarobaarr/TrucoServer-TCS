using System.Collections.Generic;
using System.Linq;
using System;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Match
{
    public static class ListPositionService
    {
        public static List<PlayerInformation> DetermineTurnOrder(List<PlayerInformation> players, string ownerUsername)
        {
            if (players == null || players.Count == 0)
            {
                return players;
            }

            return players.Count == 2
                ? OrderForDuel(players, ownerUsername)
                : OrderForTeamMatch(players, ownerUsername);
        }

        private static List<PlayerInformation> OrderForDuel(List<PlayerInformation> players, string ownerUsername)
        {
            var owner = players.FirstOrDefault(p => p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));
            var opponent = players.FirstOrDefault(p => !p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));

            if (owner == null || opponent == null)
            {
                return players;
            }

            return new List<PlayerInformation> 
            { 
                owner, opponent 
            };
        }

        private static List<PlayerInformation> OrderForTeamMatch(List<PlayerInformation> players, string ownerUsername)
        {
            var owner = players.FirstOrDefault(p => p.Username.Equals(ownerUsername, StringComparison.OrdinalIgnoreCase));
            
            if (owner == null)
            {
                return players;
            }

            var teammates = players.Where(p => p.Team == owner.Team && p.Username != ownerUsername).ToList();
            var opponents = players.Where(p => p.Team != owner.Team).OrderBy(p => p.Username).ToList();

            if (!teammates.Any() || opponents.Count < 2)
            {
                return players;
            }

            return new List<PlayerInformation>
            {
                owner,
                opponents[0],
                teammates[0],
                opponents[1]
            };
        }
    }
}