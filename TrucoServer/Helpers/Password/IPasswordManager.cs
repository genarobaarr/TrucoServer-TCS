namespace TrucoServer.Helpers.Password
{
    public interface IPasswordManager
    {
        bool UpdatePasswordAndNotify(string email, string newPassword, string languageCode, string callingMethod);
    }
}
