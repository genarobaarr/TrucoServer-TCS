using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Profanity
{
    public interface IProfanityServerService
    {
        void LoadBannedWords();

        bool ContainsProfanity(string text);

        string CensorText(string text);

        BannedWordList GetBannedWordsForClient();
    }
}
