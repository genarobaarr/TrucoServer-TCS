using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public class TrucoGameManager : IGameManager
    {
        private const string ROUND_INPROGRESS = "InProgress";
        private const string ROUND_FINISHED = "Finished";
        private const string ROUND_PLAYING = "Playing";
        private const int INITIAL_SCORE = 0;

        private static baseDatosTrucoEntities GetContext()
        {
            return new baseDatosTrucoEntities();
        }

        public int SaveMatchToDatabase(string matchCode, List<PlayerInformation> players)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = new Match
                    {
                        lobbyID = 1, // TODO: Ajustar esto al ID correspondiente
                        versionID = 1, // Esto también
                        status = ROUND_INPROGRESS,
                        startedAt = DateTime.Now
                    };

                    context.Match.Add(match);
                    context.SaveChanges();

                    foreach (var p in players)
                    {
                        var user = context.User.FirstOrDefault(u => u.username == p.Username);

                        if (user == null)
                        {
                            continue;
                        }

                        context.MatchPlayer.Add(new MatchPlayer
                        {
                            matchID = match.matchID,
                            userID = user.userID,
                            team = p.Team,
                            score = INITIAL_SCORE,
                            isWinner = false
                        });
                    }

                    context.SaveChanges();

                    var round = new Round
                    {
                        matchID = match.matchID,
                        number = 1, // TODO: Ajustar si es necesario
                        status = ROUND_PLAYING,
                        isActive = true
                    };

                    context.Round.Add(round);
                    context.SaveChanges();

                    return match.matchID;
                }
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
                return -1;
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
                return -1;
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
                return -1;
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
                return -1;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchToDatabase));
                return -1;
            }
        }

        public void SaveDealtCards(string matchCode, PlayerInformation player)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = context.Match.FirstOrDefault(m => m.status == ROUND_INPROGRESS);
                    
                    if (match == null)
                    {
                        return;
                    }
                    
                    var round = context.Round.FirstOrDefault(r => r.matchID == match.matchID && r.isActive == true);
                    
                    if (round == null)
                    {
                        return;
                    }
                    
                    var user = context.User.FirstOrDefault(u => u.username == player.Username);
                    
                    if (user == null)
                    {
                        return;
                    }
                    
                    foreach (var card in player.Hand)
                    {
                        var cardEntity = context.Card.FirstOrDefault(c =>
                            c.suit == card.CardSuit.ToString() &&
                            c.rank == card.CardRank.ToString());

                        if (cardEntity != null)
                        {
                            context.DealtCard.Add(new DealtCard
                            {
                                roundID = round.roundID,
                                playerID = user.userID,
                                cardID = cardEntity.cardID
                            });
                        }
                    }

                    context.SaveChanges();
                }
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveDealtCards));
            }
        }

        public void SaveRoundResult(string matchCode, string winner)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = context.Match.FirstOrDefault(m => m.status == ROUND_INPROGRESS);
                    
                    if (match == null)
                    {
                        return;
                    }
                    
                    var round = context.Round.FirstOrDefault(r => r.matchID == match.matchID && r.isActive == true);
                    
                    if (round == null)
                    {
                        return;
                    }
                    
                    var winnerUsername = context.User.FirstOrDefault(u => u.username == winner);

                    round.status = ROUND_FINISHED;
                    round.isActive = false;
                    round.winnerID = winnerUsername?.userID;

                    context.SaveChanges();
                }
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveRoundResult));
            }
        }

        public void SaveMatchResult(string matchCode, string loserTeam, int winnerScore, int loserScore)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = context.Match.FirstOrDefault(m => m.status == ROUND_INPROGRESS);

                    if (match == null)
                    {
                        return;
                    }

                    var players = context.MatchPlayer.Where(mp => mp.matchID == match.matchID).ToList();

                    foreach (var mp in players)
                    {
                        bool isLoser = string.Equals(mp.team.Trim(), loserTeam.Trim(), StringComparison.OrdinalIgnoreCase);

                        if (isLoser)
                        {
                            mp.isWinner = false;
                            mp.score = loserScore;
                        }
                        else
                        {
                            mp.isWinner = true;
                            mp.score = winnerScore;
                        }

                        var userStats = context.User.FirstOrDefault(u => u.userID == mp.userID);

                        if (userStats != null)
                        {
                            if (mp.isWinner)
                            {
                                userStats.wins++;
                            }
                            else
                            {
                                userStats.losses++;
                            }
                        }
                    }

                    match.status = ROUND_FINISHED;
                    match.endedAt = DateTime.Now;
                    context.SaveChanges();
                }
            }
            catch (DbEntityValidationException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (DbUpdateException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (SqlException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (InvalidOperationException ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
        }
    }
}