namespace TrucoServer.Helpers.Match
{
    public interface IMatchCodeGenerator
    {
        string GenerateMatchCode();
        int GenerateNumericCodeFromString(string code);
    }
}
