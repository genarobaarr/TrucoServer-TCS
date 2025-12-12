using System.Collections.Generic;
using TrucoServer.Contracts;

namespace TrucoServer.Data.DTOs
{
    public class MatchPlayersResult
    {
        public List<PlayerInformationWithConstructor> Players { get; set; } = new List<PlayerInformationWithConstructor>();
        public Dictionary<int, ITrucoCallback> Callbacks { get; set; } = new Dictionary<int, ITrucoCallback>();
        public bool IsSuccess => Players.Count > 0 && Players.Count == Callbacks.Count;
    }
}
