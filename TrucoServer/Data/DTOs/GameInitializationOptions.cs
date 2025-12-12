using System.Collections.Generic;
using TrucoServer.Contracts;

namespace TrucoServer.Data.DTOs
{
    public class GameInitializationOptions
    {
        public string MatchCode { get; set; }
        public int LobbyId { get; set; }
        public List<PlayerInformationWithConstructor> GamePlayers { get; set; }
        public Dictionary<int, ITrucoCallback> GameCallbacks { get; set; }
    }
}
