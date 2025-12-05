using System;
using System.Collections.Generic;
using System.Linq;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Profanity
{
    public class BannedWordRepository : IBannedWordRepository
    {
        public IEnumerable<string> GetAllWords()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    return context.BannedWord
                                  .AsNoTracking()
                                  .Select(b => b.word)
                                  .ToList();
                }
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetAllWords));
                return new List<string>();
            }
        }
    }
}
