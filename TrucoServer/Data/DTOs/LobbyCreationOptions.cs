namespace TrucoServer.Helpers.Match
{
    public class LobbyCreationOptions
    {
        public User Host { get; set; }
        public int VersionId { get; set; }
        public int MaxPlayers { get; set; }
        public string Status { get; set; }
    }
}