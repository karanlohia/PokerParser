using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PokerParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var strdate = args.Length > 0 ? args[0] : "14May";
            var pokerfolder = args.Length > 1 ? args[1] : @"C:\Users\Karan\OneDrive\temp\poker";
            var mainplayer = args.Length > 2 ? args[2] : "K-Lo";
            var games = PokerParser.Parse(pokerfolder);
            var playerstats = PokerParser.CreateGameStats(games);

            CreateGameAndSummaryReport(playerstats, Path.Combine(pokerfolder, $@"PokerParserResults_{strdate}.csv"));
            RunWaveAnalysis(games, Path.Combine(pokerfolder,$@"waveanalysis_{strdate}.csv"));
            RunFlopAnalysis(games, Path.Combine(pokerfolder, $@"flopanalysis_{strdate}.csv"));
            RunWorstLossHandsAnalysis(mainplayer, games, Path.Combine(pokerfolder, $@"{mainplayer}_losshands_{strdate}.txt"));
        }

        private static void CreateGameAndSummaryReport(Dictionary<string, PlayerStats> playerstats, string outputfile)
        {
            var pgssummaryout = new List<string>();
            var pgsout = new List<string>();
            pgsout.Add($"Player,Game Date,Game Finish,Game Pts,£££,VPIP,PFR,VPIP/PFR,CBet,3Bet,Won %,All-in 8x %");
            pgssummaryout.Add($"Player,Played,Games Won,Pts,£££,VPIP,PFR,VPIP/PFR,CBet,3Bet,Won %,All-in 8x %");

            foreach (var p in playerstats)
            {
                foreach (var pgs in p.Value.GameStats)
                {
                    pgsout.Add($"{p.Key},{pgs.GameDate:dd-MMM},{pgs.GameFinish},{pgs.FinishPoints},," +
                        $"{pgs.VpipsPercenatage:N2},{pgs.PfrsPercentage:N2}," +
                        $"{pgs.VpipsPercenatage / pgs.PfrsPercentage:N2}," +
                        $"{pgs.CBetsPercentage:N2}," +
                        $"{pgs.ThreeBetPercentage:N2}," +
                        $"{(double)pgs.HandsWon / (double)pgs.HandsPlayed:N2},{(double)pgs.Allins8bb / (double)pgs.HandsPlayed:N2}");
                }
                if (p.Value.GameStats.Count() >= 1)
                {
                    var pts = p.Value.Points;
                    double score = (double)pts / ((double)p.Value.GameStats.Count()
                        + (0.5 * (playerstats.Values.Max(v => v.GameStats.Count()) / (double)p.Value.GameStats.Count())));

                    pgssummaryout.Add($"{p.Key},{p.Value.GameStats.Count()},{p.Value.GamesWon},{pts:N2},," +
                        $"{p.Value.VPIP:N2},{p.Value.PFR:N2},{p.Value.VpipOverPfr:N2}," +
                        $"{p.Value.CBet:N2},{p.Value.ThreeBet:N2}," +
                        $"{p.Value.HandsWon:N2},{p.Value.Allin8bb:N2}");
                }
            }

            var orderedsummary = pgssummaryout.Take(1).Concat(
                pgssummaryout.Skip(1).Select(s => s.Split(',')).OrderByDescending(s => Convert.ToDouble(s[3])).Select(s => string.Join(",", s)).ToList());

            var orderedout = pgsout.Take(1).Concat(
                pgsout.Skip(1).Select(s => s.Split(',')).OrderByDescending(s => Convert.ToDateTime(s[1])).ThenBy(s => s[2]).Select(s => string.Join(",", s)).ToList());

            File.WriteAllLines(outputfile,
                orderedsummary.Concat(new List<string> { "" }).Concat(orderedout).Concat(new List<string> { "" }));
        }

        private static void RunWaveAnalysis(List<Game> games, string outputfile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Game Date,Hand,Pre-flop Pot Size,Pot Size");
            foreach (var g in games)
            {
                int i = 1;
                foreach (var h in g.Hands)
                {
                    var potsize = h.Rounds.SelectMany(r => r.Plays).Sum(p => (double)p.Chips / (double)h.BigBlind);
                    var pfpotsize = h.Rounds.Where(r => r.Street == StreetEnum.PreFlop).SelectMany(r => r.Plays).Sum(p => (double)p.Chips / (double)h.BigBlind);

                    sb.AppendLine($"{g.GameDate:dd-MMM},{i},{pfpotsize},{potsize}");
                    i++;
                }
            }
            File.WriteAllText(outputfile, sb.ToString());
        }

        private static void RunFlopAnalysis(List<Game> games, string outputfile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Game Date,Flops,PairedBoards,FlushBoards,1xFace,2xFace,3xFace");
            foreach (var g in games)
            {
                var flops = g.Hands.SelectMany(r => r.Rounds).Where(r => r.Street == StreetEnum.Flop);
                sb.AppendLine($"{g.GameDate:dd-MMM},{flops.Count()}," +
                    $"{flops.Where(f => f.StreetCardStats.IsPairedBoard).Count()}," +
                    $"{flops.Where(f => f.StreetCardStats.IsFlushBoard).Count()}," +
                    $"{flops.Where(f => f.StreetCardStats.FaceCards == 1).Count()}," +
                    $"{flops.Where(f => f.StreetCardStats.FaceCards == 2).Count()}," +
                    $"{flops.Where(f => f.StreetCardStats.FaceCards == 3).Count()},");
            }
            File.WriteAllText(outputfile, sb.ToString());
        }

        private static void RunWorstLossHandsAnalysis(string player, List<Game> games, string outputfile)
        {
            var handsplayed2bbButLost = games.Last().Hands.Where(h => h.Rounds[0].Players.Contains("K-Lo") && h.Winner != "K-Lo" && h.Rounds.SelectMany(p => p.Plays).Where(p => p.Player == "K-Lo").Sum(p => p.Chips) > h.BigBlind * 2).ToList();

            var sb = new StringBuilder();
            foreach (var h in handsplayed2bbButLost)
            {
                sb.AppendLine("******************************************************");
                sb.AppendLine($"My hand: {h.MyHand}");
                sb.AppendLine($"Big Blind: {h.BigBlind}");
                int chipslost = 0;
                foreach (var r in h.Rounds)
                {
                    string plays = "";
                    foreach (var p in r.Plays)
                    {
                        if (p.Player == "K-Lo")
                            chipslost += p.Chips;

                        if (p.Chips > 0)
                            plays = plays + $"{p.Player} {p.PlayAction} {p.Chips} | ";
                        else
                            plays = plays + $"{p.Player} {p.PlayAction} | ";
                    }
                    sb.AppendLine($"{r.Street}: {r.StreetCards} - {plays}");
                }
                sb.AppendLine($"Winner {h.Winner}, Chips lost {chipslost}");
            }
            File.WriteAllText(outputfile, sb.ToString());
        }
    }
}
