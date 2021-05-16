using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerParser
{
    public class Play
    {
        public int Chips { get; set; }
        public PlayActionEnum PlayAction { get; set; }
        public string Player { get; set; }
        public bool Allin { get; set; }
        public bool Allin8Bb { get; set; }
    }

      public enum PlayActionEnum
    {
        Call,
        Fold,
        Check,
        Raise
    }
}
