namespace TrucoServer.Data.DTOs
{
    public class FriendRequest
    {
        public int RequesterId { get; set; }
        public int TargetId { get; set; }
        public string Status { get; set; }
    }
}
