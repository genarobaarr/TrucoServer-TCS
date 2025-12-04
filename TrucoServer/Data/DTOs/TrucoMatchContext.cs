using System.Collections.Generic;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;

namespace TrucoServer.GameLogic
{
    public class TrucoMatchContext
    {
        public string MatchCode { get; set; }
        public int LobbyId { get; set; }
        public List<PlayerInformation> Players { get; set; }
        public Dictionary<int, ITrucoCallback> Callbacks { get; set; }
        public ITrucoDeck Deck { get; set; }
        public IGameManager GameManager { get; set; }
    }
}