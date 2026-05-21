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
        private readonly baseDatosTrucoEntities injectedContext;

        private static readonly Dictionary<string, Dictionary<int, ITrucoTournamentCallback>> tournamentCallbacks
            = new Dictionary<string, Dictionary<int, ITrucoTournamentCallback>>();
        private static readonly object stateLock = new object();

        public TrucoTournamentServiceImplementation() { }

        public TrucoTournamentServiceImplementation(baseDatosTrucoEntities context)
        {
            injectedContext = context;
        }

        private baseDatosTrucoEntities CreateContext()
        {
            return injectedContext ?? new baseDatosTrucoEntities();
        }

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
                            tournamentCallbacks[code][hostUserId] = hostCallback;
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
                            tournamentCallbacks[code] = new Dictionary<int, ITrucoTournamentCallback>();
                        if (joinerCallback != null)
                            tournamentCallbacks[code][userId] = joinerCallback;
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
                        return CancelTournamentInternal(ctx, tournament, "El host canceló el torneo");

                    var participant = tournament.TournamentParticipants.FirstOrDefault(p => p.UserId == userId);
                    if (participant == null) return false;

                    ctx.TournamentParticipants.Remove(participant);
                    ctx.SaveChanges();

                    var user = ctx.User.Find(userId);
                    string username = user != null ? user.username : "Unknown";

                    lock (stateLock)
                    {
                        if (tournamentCallbacks.ContainsKey(code))
                            tournamentCallbacks[code].Remove(userId);
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
                using (var ctx = CreateContext())
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

        public List<TournamentDTO> GetAvailableTournaments()
        {
            try
            {
                using (var ctx = CreateContext())
                {
                    return ctx.Tournaments
                        .Where(t => t.Status == "Waiting")
                        .Select(t => new TournamentDTO
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Capacity = t.Capacity,
                            Status = t.Status
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetAvailableTournaments));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public void ReportMatchResult(string tournamentCode, string matchCode, int winnerUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tournamentCode) || string.IsNullOrWhiteSpace(matchCode)) return;

                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.FirstOrDefault(t => t.Code == tournamentCode);
                    if (tournament == null || tournament.Status != "InProgress") return;

                    var bracket = ctx.TournamentBrackets
                        .FirstOrDefault(b => b.TournamentId == tournament.Id && b.MatchId == matchCode);
                    if (bracket == null || bracket.WinnerId.HasValue) return;
                    if (bracket.Player1Id != winnerUserId && bracket.Player2Id != winnerUserId) return;

                    int loserId = (bracket.Player1Id == winnerUserId)
                        ? bracket.Player2Id.Value
                        : bracket.Player1Id.Value;

                    bracket.WinnerId = winnerUserId;

                    var winnerUser = ctx.User.Find(winnerUserId);
                    var updatedDTO = new BracketDTO
                    {
                        Id = bracket.Id,
                        Round = bracket.Round,
                        Position = bracket.Position,
                        Player1Name = bracket.User1 != null ? bracket.User1.username : "TBD",
                        Player2Name = bracket.User2 != null ? bracket.User2.username : "TBD",
                        WinnerName = winnerUser != null ? winnerUser.username : null,
                        MatchId = bracket.MatchId
                    };

                    int matchesInFirstRound = tournament.Capacity / 2;
                    int roundStart = GetRoundStart(bracket.Round, matchesInFirstRound);
                    int relativePos = bracket.Position - roundStart;
                    int nextAbsolutePos = GetRoundStart(bracket.Round + 1, matchesInFirstRound) + (relativePos / 2);
                    bool isPlayer1Next = (relativePos % 2 == 0);

                    var nextBracket = ctx.TournamentBrackets
                        .FirstOrDefault(b => b.TournamentId == tournament.Id && b.Position == nextAbsolutePos);

                    string nextMatchCode = null;
                    if (nextBracket != null)
                    {
                        if (isPlayer1Next) nextBracket.Player1Id = winnerUserId;
                        else nextBracket.Player2Id = winnerUserId;

                        if (nextBracket.Player1Id.HasValue && nextBracket.Player2Id.HasValue)
                        {
                            nextMatchCode = codeGenerator.GenerateMatchCode();
                            nextBracket.MatchId = nextMatchCode;
                        }
                    }
                    else
                    {
                        tournament.Status = "Completed";
                    }

                    ctx.SaveChanges();

                    BroadcastWithCleanup(tournamentCode, cb => cb.OnBracketUpdated(updatedDTO));

                    if (nextMatchCode != null && nextBracket != null)
                    {
                        var p1User = ctx.User.Find(nextBracket.Player1Id);
                        var p2User = ctx.User.Find(nextBracket.Player2Id);
                        var nextBracketDTO = new BracketDTO
                        {
                            Id = nextBracket.Id,
                            Round = nextBracket.Round,
                            Position = nextBracket.Position,
                            Player1Name = p1User != null ? p1User.username : "TBD",
                            Player2Name = p2User != null ? p2User.username : "TBD",
                            WinnerName = null,
                            MatchId = nextMatchCode
                        };
                        BroadcastWithCleanup(tournamentCode, cb => cb.OnBracketUpdated(nextBracketDTO));
                    }

                    lock (stateLock)
                    {
                        if (!tournamentCallbacks.ContainsKey(tournamentCode)) return;

                        if (tournamentCallbacks[tournamentCode].TryGetValue(loserId, out var loserCb))
                            try { loserCb.OnTournamentEliminated(); } catch { }

                        if (nextMatchCode != null)
                        {
                            if (tournamentCallbacks[tournamentCode].TryGetValue(winnerUserId, out var winnerCb))
                                try { winnerCb.OnAdvanceToFinal(nextMatchCode, isPlayer1Next); } catch { }
                        }
                        else if (tournament.Status == "Completed")
                        {
                            string winnerName = winnerUser != null ? winnerUser.username : "Unknown";
                            if (tournamentCallbacks[tournamentCode].TryGetValue(winnerUserId, out var winnerCb))
                                try { winnerCb.OnTournamentChampion(winnerName); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(ReportMatchResult));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public void UpdateBracketMatchCode(string tournamentCode, string oldMatchCode, string newMatchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tournamentCode) || string.IsNullOrWhiteSpace(newMatchCode)) return;

                using (var ctx = new baseDatosTrucoEntities())
                {
                    var tournament = ctx.Tournaments.FirstOrDefault(t => t.Code == tournamentCode);
                    if (tournament == null) return;

                    var bracket = ctx.TournamentBrackets
                        .FirstOrDefault(b => b.TournamentId == tournament.Id && b.MatchId == oldMatchCode);
                    if (bracket == null) return;

                    bracket.MatchId = newMatchCode;
                    ctx.SaveChanges();

                    var updatedDTO = new BracketDTO
                    {
                        Id = bracket.Id,
                        Round = bracket.Round,
                        Position = bracket.Position,
                        Player1Name = bracket.User1 != null ? bracket.User1.username : "TBD",
                        Player2Name = bracket.User2 != null ? bracket.User2.username : "TBD",
                        WinnerName = bracket.User != null ? bracket.User.username : null,
                        MatchId = newMatchCode
                    };

                    BroadcastWithCleanup(tournamentCode, cb => cb.OnBracketUpdated(updatedDTO));
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(UpdateBracketMatchCode));
            }
        }

        private bool CancelTournamentInternal(baseDatosTrucoEntities ctx, Tournaments tournament, string reason)
        {
            string code = tournament.Code;

            var participants = tournament.TournamentParticipants.ToList();
            foreach (var p in participants)
                ctx.TournamentParticipants.Remove(p);

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
                            Player2Id = players[(i * 2) + 1].UserId,
                            MatchId = codeGenerator.GenerateMatchCode()
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

        private static int GetRoundStart(int round, int matchesInFirstRound)
        {
            int start = 0;
            int count = matchesInFirstRound;
            for (int r = 1; r < round; r++)
            {
                start += count;
                count /= 2;
            }
            return start;
        }

        private string GenerateUniqueCode(baseDatosTrucoEntities ctx)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                string candidate = codeGenerator.GenerateMatchCode();
                if (string.IsNullOrEmpty(candidate)) continue;
                if (!ctx.Tournaments.Any(t => t.Code == candidate))
                    return candidate;
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
            => BroadcastWithCleanup(code, cb => cb.OnTournamentPlayerJoined(username, count));

        private void NotifyPlayerLeft(string code, string username, int count)
            => BroadcastWithCleanup(code, cb => cb.OnTournamentPlayerLeft(username, count));

        private void NotifyTournamentStarted(string code, List<BracketDTO> tree)
            => BroadcastWithCleanup(code, cb => cb.OnTournamentStarted(tree));

        private void NotifyTournamentCancelled(string code, string reason)
            => BroadcastWithCleanup(code, cb => cb.OnTournamentCancelled(reason));

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
                try { action(entry.Value); }
                catch { dead.Add(entry.Key); }
            }

            foreach (int userId in dead)
                HandleDeadCallback(code, userId);
        }

        private void HandleDeadCallback(string code, int userId)
        {
            try
            {
                lock (stateLock)
                {
                    if (tournamentCallbacks.ContainsKey(code))
                        tournamentCallbacks[code].Remove(userId);
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