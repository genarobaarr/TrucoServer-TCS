namespace TrucoServer.Data.DTOs
{
    public class LeaveMatchContext
    {
        public string MatchCode { get; set; }
        public string PlayerUsername { get; set; }
        public Lobby Lobby { get; set; }
        public User User { get; set; }
    }
}
