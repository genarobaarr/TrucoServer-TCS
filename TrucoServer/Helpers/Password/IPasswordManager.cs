using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Password
{
    public interface IPasswordManager
    {
        bool UpdatePasswordAndNotify(PasswordUpdateOptions context);
    }
}
