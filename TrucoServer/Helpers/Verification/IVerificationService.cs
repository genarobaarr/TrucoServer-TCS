namespace TrucoServer.Helpers.Verification
{
    public interface IVerificationService
    {
        bool RequestEmailVerification(string email, string languageCode);
        bool ConfirmEmailVerification(string email, string code);
    }
}
