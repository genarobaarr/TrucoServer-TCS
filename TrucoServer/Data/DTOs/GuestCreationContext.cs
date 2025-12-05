namespace TrucoServer.Data.DTOs
{
    public class GuestCreationContext
    {
        public Lobby Lobby { get; set; }
        public string MatchCode { get; set; }
        public string PlayerUsername { get; set; }
    }
}
