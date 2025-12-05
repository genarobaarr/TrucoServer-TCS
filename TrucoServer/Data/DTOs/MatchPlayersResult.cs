using System.Collections.Generic;
using TrucoServer.Contracts;

namespace TrucoServer.Data.DTOs
{
    public class MatchPlayersResult
    {
        public List<PlayerInformation> Players { get; set; } = new List<PlayerInformation>();
        public Dictionary<int, ITrucoCallback> Callbacks { get; set; } = new Dictionary<int, ITrucoCallback>();
        public bool IsSuccess => Players.Count > 0 && Players.Count == Callbacks.Count;
    }
}
