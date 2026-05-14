using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using TrucoServer.Contracts;
using TrucoServer.Data.DTOs;
using TrucoServer.Utilities;

namespace TrucoServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class TrucoTournamentServiceImplementation : ITrucoTournamentService
    {
        private baseDatosTrucoEntities databaseContext;
        private static readonly Dictionary<int, List<ITrucoTournamentCallback>> tournamentSubscribers = new Dictionary<int, List<ITrucoTournamentCallback>>();

        public TrucoTournamentServiceImplementation()
        {
            this.databaseContext = new baseDatosTrucoEntities();
            EnsureWaitingTournamentExists();
        }

        public TrucoTournamentServiceImplementation(baseDatosTrucoEntities injectedContext)
        {
            this.databaseContext = injectedContext;
        }

        public List<TournamentDTO> GetAvailableTournaments()
        {
            try
            {
                return this.databaseContext.Tournaments
                    .Where(t => t.Status == "Waiting")
                    .Select(t => new TournamentDTO
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Capacity = t.Capacity,
                        Status = t.Status
                    }).ToList();
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetAvailableTournaments));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        public bool SubscribeToTournament(int tournamentId, int userId)
        {
            try
            {
                ITrucoTournamentCallback callback = null;

                if (OperationContext.Current != null)
                {
                    callback = OperationContext.Current.GetCallbackChannel<ITrucoTournamentCallback>();
                }

                var tournament = this.databaseContext.Tournaments.Find(tournamentId);

                if (tournament == null || tournament.Status != "Waiting")
                {
                    return false;
                }

                if (tournament.TournamentParticipants.Any(p => p.UserId == userId))
                {
                    return false;
                }

                int currentPosition = tournament.TournamentParticipants.Count();
                var participant = new TournamentParticipants
                {
                    TournamentId = tournamentId,
                    UserId = userId,
                    SeedPosition = currentPosition
                };

                this.databaseContext.TournamentParticipants.Add(participant);
                this.databaseContext.SaveChanges();

                if (callback != null)
                {
                    this.RegisterCallback(tournamentId, callback);
                }

                var user = this.databaseContext.User.Find(userId);
                string username = "Unknown";

                if (user != null)
                {
                    username = user.username;
                }

                this.NotifyPlayerJoined(tournamentId, username, tournament.TournamentParticipants.Count());

                if (tournament.TournamentParticipants.Count() == tournament.Capacity)
                {
                    this.StartTournamentLogic(tournamentId);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SubscribeToTournament));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        private void StartTournamentLogic(int tournamentId)
        {
            try
            {
                var tournament = this.databaseContext.Tournaments.Find(tournamentId);

                if (tournament != null)
                {
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
                        this.databaseContext.TournamentBrackets.Add(bracket);
                    }

                    for (int i = matchesInFirstRound; i < numMatches; i++)
                    {
                        this.databaseContext.TournamentBrackets.Add(new TournamentBrackets
                        {
                            TournamentId = tournamentId,
                            Round = (i < matchesInFirstRound + (matchesInFirstRound / 2)) ? 2 : 3,
                            Position = i
                        });
                    }

                    this.databaseContext.SaveChanges();
                    this.NotifyTournamentStarted(tournamentId, this.GetTournamentTree(tournamentId));
                    EnsureWaitingTournamentExists();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(StartTournamentLogic));
            }
        }

        public List<BracketDTO> GetTournamentTree(int tournamentId)
        {
            try
            {
                return this.databaseContext.TournamentBrackets
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(GetTournamentTree));
                throw FaultFactory.CreateFault("Error", ex.Message);
            }
        }

        private void EnsureWaitingTournamentExists()
        {
            try
            {
                bool hasWaiting = this.databaseContext.Tournaments.Any(t => t.Status == "Waiting");

                if (!hasWaiting)
                {
                    this.databaseContext.Tournaments.Add(new Tournaments
                    {
                        Name = "Torneo eliminatorio",
                        Capacity = 4,
                        Status = "Waiting",
                        CreationDate = DateTime.Now
                    });
                    this.databaseContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(EnsureWaitingTournamentExists));
            }
        }

        private void RegisterCallback(int tournamentId, ITrucoTournamentCallback callback)
        {
            if (!tournamentSubscribers.ContainsKey(tournamentId))
            {
                tournamentSubscribers[tournamentId] = new List<ITrucoTournamentCallback>();
            }

            if (!tournamentSubscribers[tournamentId].Contains(callback))
            {
                tournamentSubscribers[tournamentId].Add(callback);
            }
        }

        private void NotifyPlayerJoined(int tournamentId, string username, int count)
        {
            if (tournamentSubscribers.ContainsKey(tournamentId))
            {
                foreach (var cb in tournamentSubscribers[tournamentId])
                {
                    try
                    {
                        cb.OnTournamentPlayerJoined(username, count);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void NotifyTournamentStarted(int tournamentId, List<BracketDTO> tree)
        {
            if (tournamentSubscribers.ContainsKey(tournamentId))
            {
                foreach (var cb in tournamentSubscribers[tournamentId])
                {
                    try
                    {
                        cb.OnTournamentStarted(tree);
                    }
                    catch
                    {
                        // Ignorar canales desconectados para no bloquear el bucle
                    }
                }
            }
        }
    }
}