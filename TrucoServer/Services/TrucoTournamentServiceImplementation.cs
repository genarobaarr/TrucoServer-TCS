using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Helpers.Match;
using TrucoServer.Utilities;

namespace TrucoServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class TrucoTournamentServiceImplementation : ITrucoTournamentService
    {
        private readonly IMatchCodeGenerator codeGenerator = new MatchCodeGenerator();

        private static readonly Dictionary<string, Dictionary<int, ITrucoTournamentCallback>> tournamentCallbacks
            = new Dictionary<string, Dictionary<int, ITrucoTournamentCallback>>();
        private static readonly object stateLock = new object();

        public TrucoTournamentServiceImplementation() { }

        public string CreateTournament(int capacity, int hostUserId)
        {
            try
            {
                if (capacity != 4 && capacity != 8) return string.Empty;

                using (var ctx = new baseDatosTrucoEntities())
                {
                    if (ctx.User.Find(hostUserId) == null) return string.Empty;

                    string code = GenerateUniqueCode(ctx);
                    if (string.IsNullOrEmpty(code)) return string.Empty;

                    var tournament = new Tournaments
                    {
                        Name = "Torneo " + code,
                        Capacity = capacity,
                        Status = "Waiting",
                        CreationDate = DateTime.Now,
                        Code = code,
                        HostUserId = hostUserId
                    };
                    ctx.Tournaments.Add(tournament);
                    ctx.SaveChanges();

                    var participant = new TournamentParticipants
                    {
                        TournamentId = tournament.Id,
                        UserId = hostUserId,
                        SeedPosition = 0
                    };
                    ctx.TournamentParticipants.Add(participant);
                    ctx.SaveChanges();

                    ITrucoTournamentCallback hostCallback = GetCurrentCallback();
                    lock (stateLock)
                    {
                        tournamentCallbacks[code] = new Dictionary<int, ITrucoTournamentCallback>();
                        if (hostCallback != null)
                        {
                            tournamentCallbacks[code][hostUserId] = hostCallback;
                        }
                    }

                    return code;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(CreateTournament));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public bool JoinTournamentByCode(string code, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code)) return false;
                code = code.Trim().ToUpperInvariant();

                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.FirstOrDefault(t => t.Code == code);
                    if (tournament == null || tournament.Status != "Waiting") return false;
                    if (ctx.User.Find(userId) == null) return false;
                    if (tournament.TournamentParticipants.Any(p => p.UserId == userId)) return false;
                    if (tournament.TournamentParticipants.Count() >= tournament.Capacity) return false;

                    int currentPosition = tournament.TournamentParticipants.Count();
                    var participant = new TournamentParticipants
                    {
                        TournamentId = tournament.Id,
                        UserId = userId,
                        SeedPosition = currentPosition
                    };
                    ctx.TournamentParticipants.Add(participant);
                    ctx.SaveChanges();

                    var user = ctx.User.Find(userId);
                    string username = user != null ? user.username : "Unknown";
                    int newCount = tournament.TournamentParticipants.Count();

                    ITrucoTournamentCallback joinerCallback = GetCurrentCallback();
                    lock (stateLock)
                    {
                        if (!tournamentCallbacks.ContainsKey(code))
                        {
                            tournamentCallbacks[code] = new Dictionary<int, ITrucoTournamentCallback>();
                        }
                        if (joinerCallback != null)
                        {
                            tournamentCallbacks[code][userId] = joinerCallback;
                        }
                    }

                    NotifyPlayerJoined(code, username, newCount);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(JoinTournamentByCode));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public bool StartTournament(string code, int hostUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code)) return false;

                int tournamentId;
                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.FirstOrDefault(t => t.Code == code);
                    if (tournament == null || tournament.Status != "Waiting") return false;
                    if (tournament.HostUserId != hostUserId) return false;
                    if (tournament.TournamentParticipants.Count() != tournament.Capacity) return false;

                    tournamentId = tournament.Id;
                }

                StartTournamentLogic(tournamentId, code);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(StartTournament));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public bool LeaveTournament(string code, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code)) return false;

                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.FirstOrDefault(t => t.Code == code);
                    if (tournament == null || tournament.Status != "Waiting") return false;

                    if (tournament.HostUserId == userId)
                    {
                        return CancelTournamentInternal(ctx, tournament, "El host canceló el torneo");
                    }

                    var participant = tournament.TournamentParticipants.FirstOrDefault(p => p.UserId == userId);
                    if (participant == null) return false;

                    ctx.TournamentParticipants.Remove(participant);
                    ctx.SaveChanges();

                    var user = ctx.User.Find(userId);
                    string username = user != null ? user.username : "Unknown";

                    lock (stateLock)
                    {
                        if (tournamentCallbacks.ContainsKey(code))
                        {
                            tournamentCallbacks[code].Remove(userId);
                        }
                    }

                    NotifyPlayerLeft(code, username, tournament.TournamentParticipants.Count());
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(LeaveTournament));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public List<BracketDTO> GetTournamentTree(int tournamentId)
        {
            try
            {
                using (var ctx = new baseDatosTrucoEntities())
                {
                    return ctx.TournamentBrackets
                        .Where(b => b.TournamentId == tournamentId)
                        .OrderBy(b => b.Round).ThenBy(b => b.Position)
                        .Select(b => new BracketDTO
                        {
                            Id = b.Id,
                            Round = b.Round,
                            Position = b.Position,
                            Player1Name = b.User1 != null ? b.User1.username : "TBD",
                            Player2Name = b.User2 != null ? b.User2.username : "TBD",
                            WinnerName = b.User != null ? b.User.username : null,
                            MatchId = b.MatchId
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetTournamentTree));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        private bool CancelTournamentInternal(baseDatosTrucoEntities ctx, Tournaments tournament, string reason)
        {
            string code = tournament.Code;

            var participants = tournament.TournamentParticipants.ToList();
            foreach (var p in participants)
            {
                ctx.TournamentParticipants.Remove(p);
            }
            ctx.Tournaments.Remove(tournament);
            ctx.SaveChanges();

            NotifyTournamentCancelled(code, reason);

            lock (stateLock)
            {
                tournamentCallbacks.Remove(code);
            }
            return true;
        }

        private void StartTournamentLogic(int tournamentId, string code)
        {
            try
            {
                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.Find(tournamentId);
                    if (tournament == null) return;

                    tournament.Status = "InProgress";

                    var players = tournament.TournamentParticipants.OrderBy(x => Guid.NewGuid()).ToList();
                    int numMatches = tournament.Capacity - 1;
                    int matchesInFirstRound = tournament.Capacity / 2;

                    for (int i = 0; i < matchesInFirstRound; i++)
                    {
                        var bracket = new TournamentBrackets
                        {
                            TournamentId = tournamentId,
                            Round = 1,
                            Position = i,
                            Player1Id = players[i * 2].UserId,
                            Player2Id = players[(i * 2) + 1].UserId
                        };
                        ctx.TournamentBrackets.Add(bracket);
                    }

                    for (int i = matchesInFirstRound; i < numMatches; i++)
                    {
                        ctx.TournamentBrackets.Add(new TournamentBrackets
                        {
                            TournamentId = tournamentId,
                            Round = (i < matchesInFirstRound + (matchesInFirstRound / 2)) ? 2 : 3,
                            Position = i
                        });
                    }

                    ctx.SaveChanges();
                }

                NotifyTournamentStarted(code, GetTournamentTree(tournamentId));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(StartTournamentLogic));
            }
        }

        private string GenerateUniqueCode(baseDatosTrucoEntities ctx)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                string candidate = codeGenerator.GenerateMatchCode();
                if (string.IsNullOrEmpty(candidate)) continue;
                if (!ctx.Tournaments.Any(t => t.Code == candidate))
                {
                    return candidate;
                }
            }
            return string.Empty;
        }

        private ITrucoTournamentCallback GetCurrentCallback()
        {
            if (OperationContext.Current == null) return null;
            try
            {
                return OperationContext.Current.GetCallbackChannel<ITrucoTournamentCallback>();
            }
            catch
            {
                return null;
            }
        }

        private void NotifyPlayerJoined(string code, string username, int count)
        {
            BroadcastWithCleanup(code, cb => cb.OnTournamentPlayerJoined(username, count));
        }

        private void NotifyPlayerLeft(string code, string username, int count)
        {
            BroadcastWithCleanup(code, cb => cb.OnTournamentPlayerLeft(username, count));
        }

        private void NotifyTournamentStarted(string code, List<BracketDTO> tree)
        {
            BroadcastWithCleanup(code, cb => cb.OnTournamentStarted(tree));
        }

        private void NotifyTournamentCancelled(string code, string reason)
        {
            BroadcastWithCleanup(code, cb => cb.OnTournamentCancelled(reason));
        }

        private void BroadcastWithCleanup(string code, Action<ITrucoTournamentCallback> action)
        {
            List<KeyValuePair<int, ITrucoTournamentCallback>> snapshot;
            lock (stateLock)
            {
                if (!tournamentCallbacks.ContainsKey(code)) return;
                snapshot = tournamentCallbacks[code].ToList();
            }

            var dead = new List<int>();
            foreach (var entry in snapshot)
            {
                try
                {
                    action(entry.Value);
                }
                catch
                {
                    dead.Add(entry.Key);
                }
            }

            foreach (int userId in dead)
            {
                HandleDeadCallback(code, userId);
            }
        }

        private void HandleDeadCallback(string code, int userId)
        {
            try
            {
                lock (stateLock)
                {
                    if (tournamentCallbacks.ContainsKey(code))
                    {
                        tournamentCallbacks[code].Remove(userId);
                    }
                }

                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.FirstOrDefault(t => t.Code == code);
                    if (tournament == null || tournament.Status != "Waiting") return;

                    if (tournament.HostUserId == userId)
                    {
                        CancelTournamentInternal(ctx, tournament, "El host se desconectó");
                        return;
                    }

                    var participant = tournament.TournamentParticipants.FirstOrDefault(p => p.UserId == userId);
                    if (participant != null)
                    {
                        ctx.TournamentParticipants.Remove(participant);
                        ctx.SaveChanges();

                        var user = ctx.User.Find(userId);
                        string username = user != null ? user.username : "Unknown";
                        NotifyPlayerLeft(code, username, tournament.TournamentParticipants.Count());
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(HandleDeadCallback));
            }
        }
    }
}