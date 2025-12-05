namespace TrucoServer.Data.DTOs
{
    public class LobbyMemberDetails
    {
        public int LobbyId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public string Team { get; set; }
    }
}
