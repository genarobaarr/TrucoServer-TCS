using System.Collections.Generic;

namespace TrucoServer.Data.DTOs
{
    public class ValidatedLobbyData
    {
        public Lobby Lobby { get; set; }
        public List<LobbyMember> Members { get; set; }
        public List<PlayerInfo> Guests { get; set; }
    }
}
