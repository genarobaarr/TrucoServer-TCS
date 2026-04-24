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
        private static readonly Dictionary<int, List<ITrucoTournamentCallback>> tournamentSubscribers =
            new Dictionary<int, List<ITrucoTournamentCallback>>();

        public List<TournamentDTO> GetAvailableTournaments()
        {
            try
            {
                using (var context = new baseDatosTrucoEntities())
                {
                    return context.Tournaments
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
                throw FaultFactory.CreateFault("Error", "Error al obtener torneos disponibles");
            }
        }

        public bool SubscribeToTournament(int tournamentId, int userId)
        {
            try
            {
                ITrucoTournamentCallback callback = OperationContext.Current.GetCallbackChannel<ITrucoTournamentCallback>();

                using (var context = new baseDatosTrucoEntities())
                {
                    var tournament = context.Tournaments.Find(tournamentId);
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
                    context.TournamentParticipants.Add(participant);
                    context.SaveChanges();

                    RegisterCallback(tournamentId, callback);

                    NotifyPlayerJoined(tournamentId, context.User.Find(userId).username, tournament.TournamentParticipants.Count());

                    if (tournament.TournamentParticipants.Count() == tournament.Capacity)
                    {
                        StartTournamentLogic(tournamentId, context);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SubscribeToTournament));
                throw FaultFactory.CreateFault("Error", "Error al suscribirse a torneo");
            }
        }

        private void StartTournamentLogic(int tournamentId, baseDatosTrucoEntities context)
        {
            var tournament = context.Tournaments.Find(tournamentId);
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
                context.TournamentBrackets.Add(bracket);
            }

            for (int i = matchesInFirstRound; i < numMatches; i++)
            {
                context.TournamentBrackets.Add(new TournamentBrackets
                {
                    TournamentId = tournamentId,
                    Round = (i < matchesInFirstRound + (matchesInFirstRound / 2)) ? 2 : 3,
                    Position = i
                });
            }

            context.SaveChanges();
            NotifyTournamentStarted(tournamentId, GetTournamentTree(tournamentId));
        }

        public List<BracketDTO> GetTournamentTree(int tournamentId)
        {
            using (var context = new baseDatosTrucoEntities())
            {
                return context.TournamentBrackets
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

        #region Helpers de Callbacks
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
                        cb.OnPlayerJoined(username, count); 
                    } 
                    catch 
                    { 
                        /* Clean broken channels */ 
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
                    }
                }
            }
        }
        #endregion
    }
}