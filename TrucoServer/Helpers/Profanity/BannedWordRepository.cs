using System;
using System.Collections.Generic;
using System.Data;
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
            catch (SqlException ex)
            {
                ServerException.HandleException(ex, nameof(GetAllWords));
                
                return new List<string>();
            }
            catch (TimeoutException ex)
            {
                ServerException.HandleException(ex, nameof(GetAllWords));
               
                return new List<string>();
            }
            catch (DataException ex)
            {
                ServerException.HandleException(ex, nameof(GetAllWords));
              
                return new List<string>();
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(GetAllWords));
               
                return new List<string>();
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(GetAllWords));
              
                return new List<string>();
            }
        }
    }
}
