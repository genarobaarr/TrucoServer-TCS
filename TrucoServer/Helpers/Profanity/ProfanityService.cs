using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Utilities;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Helpers.Profanity
{
    public class ProfanityServerService : IProfanityServerService
    {
        private readonly IBannedWordRepository repository;
        private readonly HashSet<string> cachedBannedWords;
        private readonly object lockObject = new object();

        public ProfanityServerService(IBannedWordRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.cachedBannedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void LoadBannedWords()
        {
            try
            {
                var words = repository.GetAllWords();

                lock (lockObject)
                {
                    cachedBannedWords.Clear();
                    foreach (var word in words)
                    {
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            cachedBannedWords.Add(word.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(LoadBannedWords));
            }
        }

        public bool ContainsProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            lock (lockObject)
            {
                if (cachedBannedWords.Count == 0)
                {
                    return false;
                }

                var tokens = text.Split(' ');
                return tokens.Any(token => cachedBannedWords.Contains(token));
            }
        }

        public BannedWordList GetBannedWordsForClient()
        {
            lock (lockObject)
            {
                return new BannedWordList
                {
                    BannedWords = cachedBannedWords.ToList()
                };
            }
        }
    }
}
