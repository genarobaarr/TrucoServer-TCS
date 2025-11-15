using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public class TrucoGameManager : IGameManager
    {
        private baseDatosTrucoEntities GetContext()
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
                        lobbyID = 1,
                        versionID = 1,
                        status = "InProgress",
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
                            score = 0,
                            isWinner = false
                        });
                    }

                    context.SaveChanges();

                    var round = new Round
                    {
                        matchID = match.matchID,
                        number = 1,
                        status = "Playing",
                        isActive = true
                    };

                    context.Round.Add(round);
                    context.SaveChanges();

                    return match.matchID;
                }
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
                    var match = context.Match.FirstOrDefault(m => m.status == "InProgress");
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
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveDealtCards));
            }
        }

        public void SaveRoundResult(string matchCode, string winnerUsername)
        {
            try
            {
                using (var context = GetContext())
                {
                    var match = context.Match.FirstOrDefault(m => m.status == "InProgress");
                    if (match == null)
                    {
                        return;
                    }
                    var round = context.Round.FirstOrDefault(r => r.matchID == match.matchID && r.isActive == true);
                    if (round == null)
                    {
                        return;
                    }
                    var winner = context.User.FirstOrDefault(u => u.username == winnerUsername);

                    round.status = "Finished";
                    round.isActive = false;
                    round.winnerID = winner?.userID;

                    context.SaveChanges();
                }
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
                    var match = context.Match.FirstOrDefault(m => m.status == "InProgress");
                    if (match == null)
                    {
                        return;
                    }
                    
                    var players = context.MatchPlayer.Where(mp => mp.matchID == match.matchID).ToList();

                    foreach (var mp in players)
                    {
                        if (mp.team == loserTeam)
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
                            if (mp.isWinner == true)
                            {
                                userStats.wins++;
                            }
                            else
                            {
                                userStats.losses++;
                            }
                        }
                    }

                    match.status = "Finished";
                    match.endedAt = DateTime.Now;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex, nameof(SaveMatchResult));
            }
        }
    }
}