namespace TrucoServer.Data.DTOs
{
    public class AccessMatchResult
    {
        public bool IsAllowed { get; set; }
        public Invitation UsedInvitation { get; set; }
    }
}
