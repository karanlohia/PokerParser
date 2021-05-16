using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerParser
{
    public class Hand
    {
        public int SmallBlind { get; set; }
        public int BigBlind { get; set; }
        public string MyHand { get; set; }

        public List<Round> Rounds { get; set; } = new List<Round>();

        public string Winner { get; set; } //what about split pot? pah mark as split.

        public bool WinnerCardsShown { get; set; }
    }
}
