using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
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
            catch (EntityCommandExecutionException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetAllWords)} - Entity Framework Error");
                return new List<string>();
            }
            catch (EntityException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetAllWords)} - Entity Framework Error");
                return new List<string>();
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, $"{nameof(GetAllWords)} - DataBase Error");
                return new List<string>();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetAllWords));
                return new List<string>();
            }
        }
    }
}
