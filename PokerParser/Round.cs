using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerParser
{
    public class Round
    {
        public StreetEnum Street { get; set; }

        public string StreetCards { get; set; }

        public StreetCardStats StreetCardStats { get; private set; }

        public List<Play> Plays { get; set; } = new List<Play>();

        public List<string> Players { get; set; } = new List<string>();

        public void ParseStreetCards()
        {
            var cards = StreetCards.Replace("[", "").Replace("]", "");
            var cardsarr = cards.Split(',');

            var numdict = new Dictionary<string, int>();
            var suitdict = new Dictionary<string, int>();
            foreach (var c in cardsarr)
            {
                var num = c.Trim().Substring(0, c.Trim().Length - 1);
                if (!numdict.ContainsKey(num))
                    numdict.Add(num, 1);
                else
                    numdict[num]++;

                var suit = c.Trim().Substring(c.Trim().Length - 1, 1);
                if (!suitdict.ContainsKey(suit))
                    suitdict.Add(suit, 1);
                else
                    suitdict[suit]++;
            }

            StreetCardStats = new StreetCardStats();
            StreetCardStats.IsPairedBoard = numdict.Any(v => v.Value > 1);
            StreetCardStats.IsFlushBoard = suitdict.Keys.Count() == 1;
            StreetCardStats.FaceCards = numdict.Where(k => !Int32.TryParse(k.Key, out var i)).Sum(v => v.Value);

        }
    }

    public class StreetCardStats
    {
        public bool IsPairedBoard { get; set; }
        public int FaceCards { get; set; }
        public bool IsFlushBoard { get; set; }
    }

    public enum StreetEnum
    {
        PreFlop,
        Flop,
        Turn,
        River
    }
}
