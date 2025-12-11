using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.ServiceModel;
using TrucoServer.GameLogic;
using TrucoServer.Utilities;

namespace TrucoServer.Helpers.Match
{
    public class GameRegistry : IGameRegistry
    {
        private readonly ConcurrentDictionary<string, TrucoMatch> runningGames = new ConcurrentDictionary<string, TrucoMatch>();

        public bool TryAddGame(string matchCode, TrucoMatch match)
        {
            if (!runningGames.TryAdd(matchCode, match))
            {
                ServerException.HandleException(new Exception($"Failed to add running game {matchCode}"), nameof(TryAddGame));
               
                return false;
            }
            return true;
        }

        public bool TryGetGame(string matchCode, out TrucoMatch match)
        {
            return runningGames.TryGetValue(matchCode, out match);
        }

        public bool TryRemoveGame(string matchCode)
        {
            return runningGames.TryRemove(matchCode, out _);
        }

        public void AbortAndRemoveGame(string matchCode, string player)
        {
            if (runningGames.TryGetValue(matchCode, out var match))
            {
                try
                {
                    match.AbortMatch(player);
                }
                catch (SqlException ex)
                {
                    ServerException.HandleException(ex, nameof(AbortAndRemoveGame));
                }
                catch (TimeoutException ex)
                {
                    ServerException.HandleException(ex, nameof(AbortAndRemoveGame));
                }
                catch (CommunicationException ex)
                {
                    ServerException.HandleException(ex, nameof(AbortAndRemoveGame));
                }
                catch (Exception ex)
                {
                    ServerException.HandleException(ex, nameof(AbortAndRemoveGame));
                }

                runningGames.TryRemove(matchCode, out _);
            }
        }
    }
}
