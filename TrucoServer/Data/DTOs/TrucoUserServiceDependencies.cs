using TrucoServer.Helpers.Authentication;
using TrucoServer.Helpers.Email;
using TrucoServer.Helpers.Mapping;
using TrucoServer.Helpers.Password;
using TrucoServer.Helpers.Profiles;
using TrucoServer.Helpers.Ranking;
using TrucoServer.Helpers.Security;
using TrucoServer.Helpers.Sessions;
using TrucoServer.Helpers.Verification;

namespace TrucoServer.Data.DTOs
{
    public class TrucoUserServiceDependencies
    {
        public IUserAuthenticationHelper AuthenticationHelper { get; set; }
        public IUserSessionManager SessionManager { get; set; }
        public IEmailSender EmailSender { get; set; }
        public IVerificationService VerificationService { get; set; }
        public IProfileUpdater ProfileUpdater { get; set; }
        public IPasswordManager PasswordManager { get; set; }
        public IUserMapper UserMapper { get; set; }
        public IRankingService RankingService { get; set; }
        public IMatchHistoryService MatchHistoryService { get; set; }
        public BanService BanService { get; set; }
    }
}
