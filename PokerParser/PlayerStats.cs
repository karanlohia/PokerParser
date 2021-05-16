using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerParser
{
    public class PlayerStats
    {
        public List<PlayerGameStats> GameStats { get; set; } 

        public int GamesWon { get { return GameStats.Where(g => g.GameFinish == 1).Count(); } }

        public double Points { get { return GameStats.Sum(s => s.FinishPoints); } }

        public PlayerStats ()
	    {
            GameStats = new List<PlayerGameStats>();
	    }

        public double HandsWon {  get
            {
                return GameStats.Sum(g => g.HandsWon) / (double)GameStats.Sum(g => g.HandsPlayed);
            } 
        }

        public double HandsWonNoShow
        {
            get
            {
                return GameStats.Sum(g => g.HandsWonNoShow) / (double)GameStats.Sum(g => g.HandsWon);
            }
        }

        public double Allin
        {
            get
            {
                return GameStats.Sum(g => g.Allins) / (double)GameStats.Sum(g => g.HandsPlayed);
            }
        }


        public double Allin8bb
        {
            get
            {
                return GameStats.Sum(g => g.Allins8bb) / (double)GameStats.Sum(g => g.HandsPlayed);
            }
        }


        public double VPIP
        {
            get
            {
                return GameStats.Sum(g => g.Vpips) / (double)GameStats.Sum(g => g.HandsPlayed);
            }
        }

        public double PFR
        {
            get
            {
                return GameStats.Sum(g => g.Pfrs) / (double)GameStats.Sum(g => g.HandsPlayed);
            }
        }

        public double VpipOverPfr
        {
            get
            {
                if (PFR == 0d) return 0d;
                return VPIP / PFR;
            }
        }

        public double CBet
        {
            get
            {
                if (GameStats.Sum(g => g.Pfrs) == 0d) return 0d;
                return GameStats.Sum(g => g.CBet) / (double)GameStats.Sum(g => g.Pfrs);
            }
        }

        public double ThreeBet
        {
            get
            {
                return GameStats.Sum(g => g.ThreeBet) / (double)GameStats.Sum(g => g.HandsPlayed);
            }
        }

        public double Raised
        {
            get
            {
                return GameStats.Sum(g => g.Raised) / (double)GameStats.Sum(g => g.HandsPlayed);
            }
        }

    }

    public class PlayerGameStats
    {
        public string Player { get; set; }

        public DateTime GameDate { get; set; }

        public int GameFinish { get; set; }

        public double FinishPoints { get; set; }

        public int HandsPlayed { get; set; }

        public int HandsWon { get; set; }

        public int HandsWonNoShow { get; set; }

        public int Allins { get; set; }
        public int Allins8bb { get; set; }
        public int Raised { get; set; }
        public int ThreeBet { get; set; }
        public int CBet { get; set; }

        public int Vpips { get; set; }

        public int Pfrs { get; set; }

        public double VpipsPercenatage { get { return ((double)Vpips) / ((double)HandsPlayed); } }
        public double PfrsPercentage { get { return ((double)Pfrs) / ((double)HandsPlayed); } }
        public double CBetsPercentage { 
            get 
            {
                if (Pfrs == 0d) return 0d;
                return ((double)CBet) / ((double)Pfrs); 
            } 
        }
        public double ThreeBetPercentage { get { return ((double)ThreeBet) / ((double)HandsPlayed); } }

        public static double GetFinishPoints(int gameFinish, int playerscount)
        {
            return (Math.Pow(playerscount - gameFinish, 2) - playerscount) / (((0.66 + (2 / 9)) - (2 / playerscount)) * playerscount);
        }
    }
}
