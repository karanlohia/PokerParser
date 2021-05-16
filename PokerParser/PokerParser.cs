using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PokerParser
{
    public static class PokerParser
    {
        public static List<Game> Parse(string pokerfolder)
        {
            var data = new List<string[]>();

            foreach (var gamefile in Directory.EnumerateFiles(Path.Combine(pokerfolder, @"games"), "*.csv").Select(f => new FileInfo(f)).OrderBy(f => f.LastWriteTime))
            {
                data.AddRange(File.ReadLines(gamefile.FullName).Skip(1).Select(l => SplitCSV(l).ToArray()).OrderBy(l => l[1]));
            }

            var games = new List<Game>();
            Game currgame = null;
            StreetEnum currstreet = StreetEnum.PreFlop;
            foreach (var line in data.Skip(1))
            {
                var logline = line[0];
                var dateline = DateTime.Parse(line[1]);
                if (dateline > (currgame?.GameDate.AddDays(1) ?? DateTime.MinValue))
                {
                    //start a new game
                    currgame = new Game();
                    currgame.GameDate = dateline;
                    games.Add(currgame);
                    continue;
                }

                if (logline.Contains("posts a big blind"))
                {
                    //start a hand
                    var hand = new Hand();
                    if (Int32.TryParse(logline.Replace("\"", "").Split(' ').Last(), out var bb))
                        hand.BigBlind = bb;
                    currgame.Hands.Add(hand);
                    currstreet = StreetEnum.PreFlop;
                    hand.Rounds.Add(new Round() { Street = currstreet });
                    continue;
                }

                if (logline.Contains("Your hand is") && currgame.Hands.Any())
                {
                    currgame.Hands.Last().MyHand = logline.Replace("Your hand is ", "").Replace("\"", "");
                }

                if (currgame != null && currgame.Hands.Count > 0)
                {
                    if (logline.Contains("lop:"))
                    {
                        currstreet = StreetEnum.Flop;
                        currgame.Hands.Last().Rounds.Add(new Round() { Street = currstreet });
                        currgame.Hands.Last().Rounds.Last().Street = currstreet;
                        currgame.Hands.Last().Rounds.Last().StreetCards = logline.Replace("flop:", "").Replace("\"", "");
                        currgame.Hands.Last().Rounds.Last().ParseStreetCards();
                    }
                    else if (logline.Contains("urn:"))
                    {
                        currstreet = StreetEnum.Turn;
                        currgame.Hands.Last().Rounds.Add(new Round() { Street = currstreet });
                        currgame.Hands.Last().Rounds.Last().Street = currstreet;
                        currgame.Hands.Last().Rounds.Last().StreetCards = logline.Replace("turn:", "").Replace("\"", "");
                    }
                    else if (logline.Contains("iver:"))
                    {
                        currstreet = StreetEnum.River;
                        currgame.Hands.Last().Rounds.Add(new Round() { Street = currstreet });
                        currgame.Hands.Last().Rounds.Last().Street = currstreet;
                        currgame.Hands.Last().Rounds.Last().StreetCards = logline.Replace("river:", "").Replace("\"", "");
                    }
                    else if (logline.Contains(" wins ") || logline.Contains(" gained ") || logline.Contains(" collected "))
                    {
                        var fullplayer = logline.Substring(1, logline.LastIndexOf("\"\""));
                        var player = LookupPlayer(fullplayer.Split('@')[0].Trim().Replace("\"", ""));
                        currgame.Hands.Last().Winner = player;
                        currgame.Hands.Last().WinnerCardsShown = (logline.Contains(" wins ") && !logline.Contains(" gained ")) ||
                            (logline.Contains(" collected ") && !logline.Contains(" from pot with"));
                    }
                    else if (logline.Contains(" quits the game with a stack "))
                    {
                        var fullplayer = logline.Replace("The player ", "").Substring(1, logline.LastIndexOf("\"\""));
                        var player = LookupPlayer(fullplayer.Split('@')[0].Trim().Replace("\"", ""));

                        if (currgame.PlayerOutOrder.Contains(player))
                            currgame.PlayerOutOrder.Remove(player);

                        currgame.PlayerOutOrder.Add(player);
                    }
                    else if (logline.Contains("The admin approved the player "))
                    {
                        //rebuy 
                        var fullplayer = logline.Replace("The admin approved the player ", "").Substring(1, logline.LastIndexOf("\"\""));
                        var player = LookupPlayer(fullplayer.Split('@')[0].Trim().Replace("\"", ""));

                        if (currgame.PlayerOutOrder.Contains(player))
                            currgame.PlayerOutOrder.Remove(player);
                    }
                    else
                    {
                        if (logline.StartsWith("\"\"") &&
                            (logline.Contains("call") || logline.Contains("fold") || logline.Contains("check") || logline.Contains("raise") || logline.Contains("bets")))
                        {
                            var fullplayer = logline.Substring(1, logline.LastIndexOf("\"\""));
                            var player = LookupPlayer(fullplayer.Split('@')[0].Trim().Replace("\"", ""));

                            if (!currgame.Players.Contains(player))
                                currgame.Players.Add(player);

                            if (!currgame.Hands.Last().Rounds.Last().Players.Contains(player))
                                currgame.Hands.Last().Rounds.Last().Players.Add(player);

                            bool isallin = false;
                            if (logline.Contains(" and go all in"))
                            {
                                isallin = true;
                                logline = logline.Replace(" and go all in", "");
                            }

                            var playline = logline.Substring(logline.LastIndexOf("\"\"") + 1, logline.Length - (logline.LastIndexOf("\"\"") + 1)).Replace("\"", "");
                            var playlinesplit = playline.Split(' ');
                            var action = ParseAction(playlinesplit[1]);



                            int chips = 0;
                            if (playlinesplit.Count() > 2)
                                chips = Convert.ToInt32(playlinesplit.Last());

                            bool isallin8bb = false;
                            if (chips > 0 && currgame.Hands.First().BigBlind > 0 && chips / currgame.Hands.First().BigBlind >= 8 && isallin)
                                isallin8bb = true;

                            var play = new Play() { Chips = chips, PlayAction = action, Player = player, Allin = isallin, Allin8Bb = isallin8bb };
                            currgame.Hands.Last().Rounds.Last().Plays.Add(play);
                        }
                    }
                }
            }

            return games;

        }

        public static Dictionary<string, PlayerStats> CreateGameStats(List<Game> games)
        {
            var playerstats = new Dictionary<string, PlayerStats>();
            foreach (var game in games)
            {
                var places = game.GetFinishPlaces();
                foreach (var player in game.Players)
                {
                    var pgs = new PlayerGameStats() { Player = player, GameDate = game.GameDate };
                    if (!playerstats.ContainsKey(player))
                        playerstats.Add(player, new PlayerStats());

                    playerstats[player].GameStats.Add(pgs);
                    pgs.GameFinish = places.Contains(player) ? places.IndexOf(player) + 1 : places.Count();
                    pgs.FinishPoints = PlayerGameStats.GetFinishPoints(pgs.GameFinish, game.Players.Count);

                    foreach (var hand in game.Hands.Where(h => h.Winner != null))
                    {
                        bool notheadsup = hand.Rounds.First().Players.Count() > 2;
                        if (notheadsup)
                        {
                            var allplaysbyplayer = hand.Rounds.SelectMany(r => r.Plays).Where(p => p.Player.Equals(player));

                            if (hand.Rounds[0].Plays.Any(p => p.Player.Equals(player)))
                                pgs.HandsPlayed++;
                            if (hand.Rounds[0].Plays.Any(p => p.Player.Equals(player) &&
                            ((hand.BigBlind > 0 && p.Chips > hand.BigBlind / 2) || (hand.BigBlind == 0 && p.Chips > 0))))
                                pgs.Vpips++;
                            if (hand.Rounds[0].Plays.Any(p => p.Player.Equals(player) && p.PlayAction == PlayActionEnum.Raise))
                            {
                                pgs.Pfrs++;
                                if (hand.Rounds.Count > 1 &&
                                    hand.Rounds[1].Plays.Any(p => p.Player.Equals(player) && p.PlayAction == PlayActionEnum.Raise))
                                    pgs.CBet++;
                            }
                            if (hand.Rounds.Any(r => r.Plays.Any(p => p.Player.Equals(player) && p.PlayAction == PlayActionEnum.Raise)))
                                pgs.Raised++;
                            if (hand.Winner.Equals(player))
                                pgs.HandsWon++;
                            if (hand.Winner.Equals(player) && !hand.WinnerCardsShown)
                                pgs.HandsWonNoShow++;
                            if (allplaysbyplayer.Any(p => p.Allin))
                                pgs.Allins++;
                            if (allplaysbyplayer.Any(p => p.Allin8Bb))
                                pgs.Allins8bb++;
                        }
                        //lets find a 3bet
                        foreach (var r in hand.Rounds)
                        {
                            if (notheadsup)
                            {
                                bool otherraiser = false;
                                foreach (var p in r.Plays)
                                {
                                    if (p.PlayAction == PlayActionEnum.Raise)
                                    {
                                        if (p.Player.Equals(player))
                                        {
                                            if (otherraiser)
                                                pgs.ThreeBet++;
                                        }
                                        else
                                        {
                                            otherraiser = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return playerstats;
        }

        public static PlayActionEnum ParseAction(string act)
        {
            if (act == "calls") return PlayActionEnum.Call;
            if (act == "checks") return PlayActionEnum.Check;
            if (act == "folds") return PlayActionEnum.Fold;
            if (act == "raises" || act == "bets") return PlayActionEnum.Raise;

            return PlayActionEnum.Check;
        }

        public static string LookupPlayer(string player)
        {
            if (player.Equals("Air K-Lo")) return "K-Lo";

            if (player.StartsWith("Tej")) return "Tej";

            if (player.StartsWith("Arash", StringComparison.OrdinalIgnoreCase)) return "Arash";

            if (player.StartsWith("Leon")) return "Leon";

            if (player.Equals("N-Dog")) return "Neil";

            if (player.Equals("Ade")) return "Mankie";

            if (player.Equals("Adrian")) return "Mankie";
            if (player.Equals("Andonis")) return "Don";

            if (player.Equals("Sharking")) return "Harry";
            return player;
        }

        public static IEnumerable<string> SplitCSV(string input)
        {
            Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

            foreach (Match match in csvSplit.Matches(input))
            {
                yield return match.Value.TrimStart(',');
            }
        }
    }
}
