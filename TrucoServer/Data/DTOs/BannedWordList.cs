using System.Collections.Generic;

namespace TrucoServer.Data.DTOs
{
    public class BannedWordList
    {
        public List<string> BannedWords { get; set; }

        public BannedWordList()
        {
            BannedWords = new List<string>();
        }
    }
}
