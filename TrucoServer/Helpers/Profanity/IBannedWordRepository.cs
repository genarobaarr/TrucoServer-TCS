using System.Collections.Generic;

namespace TrucoServer.Helpers.Profanity
{
    public interface IBannedWordRepository
    {
        IEnumerable<string> GetAllWords();
    }
}
