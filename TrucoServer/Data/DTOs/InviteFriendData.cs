namespace TrucoServer.Data.DTOs
{
    public class InviteFriendData
    {
        public string MatchCode { get; set; }
        public User SenderUser { get; set; }
        public User FriendUser { get; set; }
    }
}
