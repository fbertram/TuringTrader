using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TuringTrader.Simulator;

namespace TuringTrader.BooksAndPubs
{
    public class RunAll : Algorithm
    {
        private readonly List<string> ALGORITHMS = new List<string>
        {
            // Aeromir Parking Trade
            // Alvarez Etf Sector Rotation
            "algorithm:Antonacci_DualMomentumInvesting",
            //"algorithm:Bensdorp_30MinStockTrader_WR",
            // Clenow Stocks on the Move
            // Connors High Probablility Etf Trading
            // Connors Short Term Trading
            // Faber Ivy Portfolio
            "algorithm:Keller_CAA_N8_TV5",
            // Keller DAA
            // Livingston Muscular Portfolios
            "algorithm:Livingston_MuscularPortfolios_MamaBear",
            Globals.BALANCED_PORTFOLIO,
        };

        private Plotter plotter = null;

        public RunAll()
        {
            plotter = new Plotter(this);
        }

        public override void Run()
        {
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            AddDataSources(ALGORITHMS);

            foreach (var simTime in SimTimes)
            {
                if (Instruments.Count() < ALGORITHMS.Count)
                    continue;

                plotter.SelectChart("Strategies from Books & Pubs", "Date");
                plotter.SetX(SimTime[0]);

                foreach (var i in Instruments)
                    plotter.Plot(i.Name, i.Close[0]);
            }
        }

        public override void Report()
        {
            plotter.OpenWith("SimpleReport");
        }
    }
}
