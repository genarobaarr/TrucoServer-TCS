namespace TrucoServer.Data.DTOs
{
    public class MatchStartValidation
    {
        public bool IsValid { get; set; }
        public int LobbyId { get; set; }
        public int ExpectedPlayers { get; set; }
    }
}
