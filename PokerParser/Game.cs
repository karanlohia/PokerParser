using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerParser
{
    public class Game
    {
        public DateTime GameDate { get; set; }

        public List<string> Players { get; set; } 

        public List<Hand> Hands { get; set; } 

        public string Winner
        {
            get
            {
                return Hands.Where(r => r.Winner != null).Last().Winner;
            }
        }

        public List<string> PlayerOutOrder { get; set; } = new List<string>();

        public List<string> GetFinishPlaces() 
        {
            var rev = new List<string>(PlayerOutOrder);
            rev.Reverse();
            
            return PlayerOutOrder.Contains(Winner) ? rev : new[] { Winner }.Concat( rev).ToList();
        }

        public Game ()
	    {
            Players = new List<string>();
            Hands = new List<Hand>();
	    }
    }
    
}
