using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Match;
using TrucoServer.Helpers.Profanity;

namespace TrucoServer.Data.DTOs
{
    public class TrucoMatchServiceDependencies
    {
        public IGameRegistry GameRegistry { get; set; }
        public IJoinService JoinService { get; set; }
        public ILobbyCoordinator LobbyCoordinator { get; set; }
        public ILobbyRepository LobbyRepository { get; set; }
        public IMatchCodeGenerator CodeGenerator { get; set; }
        public IMatchStarter Starter { get; set; }
        public IProfanityServerService ProfanityService { get; set; }
        public IEmailSender EmailSender { get; set; }
    }
}
