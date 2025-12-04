namespace TrucoServer.Data.DTOs
{
    public class FriendshipCommitOptions
    {
        public Friendship Request { get; set; }
        public int RequesterId { get; set; }
        public int AcceptorId { get; set; }
        public string StatusAccepted { get; set; }
    }
}
