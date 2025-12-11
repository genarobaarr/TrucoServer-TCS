using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Profanity
{
    public class ProfanityServerService : IProfanityServerService
    {
        private readonly IBannedWordRepository repository;
        private readonly HashSet<string> cachedBannedWords;
        private static readonly TimeSpan regexTimeout = TimeSpan.FromSeconds(0.5);
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

                if (words == null)
                {
                    return;
                }

                lock (lockObject)
                {
                    cachedBannedWords.Clear();

                    cachedBannedWords.UnionWith(
                        words.Where(word => !string.IsNullOrWhiteSpace(word))
                             .Select(word => word.Trim())
                    );
                }
            }
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(LoadBannedWords));
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(LoadBannedWords));
            }
            catch (ArgumentNullException ex)
            {
                ServerException.HandleException(ex, nameof(LoadBannedWords));
            }
            catch (OutOfMemoryException ex)
            {
                ServerException.HandleException(ex, nameof(LoadBannedWords));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(LoadBannedWords));
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

        public string CensorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string processedText = text;
            List<string> wordsFound;

            lock (lockObject)
            {
                wordsFound = cachedBannedWords
                    .Where(word => processedText.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            foreach (var badWord in wordsFound)
            {
                string pattern = $@"\b{Regex.Escape(badWord)}\b";
                string replacement = new string('*', badWord.Length);

                try
                {
                    processedText = Regex.Replace(processedText, pattern, replacement, RegexOptions.IgnoreCase, regexTimeout);
                }
                catch (Exception)
                {
                    /**
                     * The error is intentionally ignored in 
                     * order to attempt to process the next word
                     */
                }
            }

            return processedText;
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
