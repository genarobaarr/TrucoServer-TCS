namespace TrucoServer.Data.DTOs
{
    public class UsernameUpdateContext
    {
        public User User { get; set; }
        public string NewUsername { get; set; }
        public int MaxNameChanges { get; set; }
    }
}
