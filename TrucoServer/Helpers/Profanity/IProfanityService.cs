using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Profanity
{
    public interface IProfanityServerService
    {
        void LoadBannedWords();

        bool ContainsProfanity(string text);

        BannedWordList GetBannedWordsForClient();
    }
}
