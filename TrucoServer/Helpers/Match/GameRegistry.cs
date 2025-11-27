using System;
using System.Collections.Concurrent;
using TrucoServer.Utilities;
using TrucoServer.GameLogic;

namespace TrucoServer.Helpers.Match
{
    public class GameRegistry : IGameRegistry
    {
        private readonly ConcurrentDictionary<string, TrucoMatch> runningGames = new ConcurrentDictionary<string, TrucoMatch>();

        public bool TryAddGame(string matchCode, TrucoMatch match)
        {
            if (!runningGames.TryAdd(matchCode, match))
            {
                LogManager.LogError(new Exception($"Failed to add running game {matchCode}"), nameof(TryAddGame));
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
                catch (Exception ex) 
                { 
                    LogManager.LogError(ex, nameof(AbortAndRemoveGame)); 
                }

                runningGames.TryRemove(matchCode, out _);
            }
        }
    }
}
