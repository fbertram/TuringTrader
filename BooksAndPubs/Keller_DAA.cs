//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_DAA
// Description: Strategy, as published in Wouter J. Keller and Jan Willem Keuning's
//              paper 'Breadth Momentum and the Canary Universe:
//              Defensive Asset Allocation (DAA)'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=3212862
// History:     2019ii18, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace BooksAndPubs
{
    public abstract class Keller_DAA : Algorithm
    {
        #region internal data
        private readonly double INITIAL_FUNDS = 100000;
        private readonly string BENCHMARK = "@60_40";
        private Instrument _benchmark = null;
        private Plotter _plotter = new Plotter();

        protected List<string> riskyUniverse = null;
        protected List<string> cashUniverse = null;
        protected List<string> protectiveUniverse = null;
        protected int T = 0; // (risky) top parameter
        protected int B = 0; // breadth parameter
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            foreach (string nick in riskyUniverse.Concat(cashUniverse).Concat(protectiveUniverse))
                AddDataSource(nick);
            AddDataSource(BENCHMARK);

            Deposit(INITIAL_FUNDS);
            //CommissionPerShare = 0.015; // paper does not consider trade commissions

            _plotter.Clear();

            //----- simulation loop

            foreach (DateTime simTime in SimTimes)
            {
                // calculate 13612W momentum for all instruments
                Dictionary<Instrument, double> momentum13612W = Instruments
                    .ToDictionary(
                        i => i,
                        i => 0.25 *
                            (12.0 * (i.Close[0] / i.Close[21] - 1.0)
                            + 4.0 * (i.Close[0] / i.Close[63] - 1.0)
                            + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                            + 1.0 * (i.Close[0] / i.Close[252] - 1.0)));

                // skip if there are any missing instruments
                // we want to make sure our strategy has all instruments available
                bool instrumentsMissing = riskyUniverse.Concat(cashUniverse).Concat(protectiveUniverse)
                    .Where(n => Instruments.Where(i => i.Nickname == n).Count() == 0)
                    .Count() > 0;

                if (instrumentsMissing)
                    continue;

                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);

                // find T top risky assets
                IEnumerable<Instrument> topInstruments = Instruments
                    .Where(i => riskyUniverse.Contains(i.Nickname))
                    .OrderByDescending(i => momentum13612W[i])
                    .Take(T);

                // find single cash/ bond asset
                Instrument cashInstrument = Instruments
                    .Where(i => cashUniverse.Contains(i.Nickname))
                    .OrderByDescending(i => momentum13612W[i])
                    .First();

                // determine number of bad assets in canary universe
                double b = Instruments
                    .Where(i => protectiveUniverse.Contains(i.Nickname))
                    .Sum(i => momentum13612W[i] < 0.0 ? 1.0 : 0.0);

                // calculate cash fraction
                //double CF = Math.Min(1.0, b / B) // standard calculation
                double CF = Math.Min(1.0, 1.0 / T * Math.Floor(b * T / B)); // Easy Trading

                // set instrument weights
                Dictionary<Instrument, double> weights = Instruments
                    .ToDictionary(i => i, i => 0.0);

                weights[cashInstrument] = CF;

                foreach (Instrument i in topInstruments)
                    weights[i] += (1.0 - CF) / T;

                // rebalance once per month
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    foreach (Instrument i in Instruments)
                    {
                        int targetShares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);

                        Order newOrder = i.Trade(targetShares - i.Position);

                        if (newOrder != null)
                        {
                            if (i.Position == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }
                }

                if (TradingDays > 0)
                {
                    // create plots on Sheet 1
                    _plotter.SelectChart(Name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0]);
                    _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);

                    // holdings on Sheet 2
                    _plotter.SelectChart(Name + " holdings", "date");
                    _plotter.SetX(SimTime[0]);
                    foreach (var i in Positions.Keys)
                        _plotter.Plot(i.Symbol, i.Position * i.Close[0] / NetAssetValue[0]);

                }
            }

            //----- post processing

            // create trading log on Sheet 3
            _plotter.SelectChart(Name + " trades", "date");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("type", entry.InstrumentType);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("fill", entry.FillPrice);
                _plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                _plotter.Plot("commission", -entry.Commission);
                _plotter.Plot("net", -entry.OrderTicket.Quantity * entry.FillPrice - entry.Commission);
                _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }

            // calculate Keller ratio
            double R = Math.Exp(
                252.0 / TradingDays * Math.Log(NetAssetValue[0] / INITIAL_FUNDS));
            double K50 = NetAssetValueMaxDrawdown < 0.5 && R > 0.0
                ? R * (1.0 - NetAssetValueMaxDrawdown / (1.0 - NetAssetValueMaxDrawdown))
                : 0.0;
            double K25 = NetAssetValueMaxDrawdown < 0.25 && R > 0.0
                ? R * (1.0 - 2.0 * NetAssetValueMaxDrawdown / (1.0 - 2 * NetAssetValueMaxDrawdown))
                : 0.0;

            FitnessValue = K25;
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }

    #region DAA-G4 - has subpar risk/ return
    public class Keller_DAA_G4 : Keller_DAA
    {
        public Keller_DAA_G4()
        {
            riskyUniverse = new List<string>
            {
                "SPY", // SPDR S&P 500 ETF
                "VEA", // Vanguard FTSE Developed Markets ETF
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND", // Vanguard Total Bond Market ETF
            };

            cashUniverse = new List<string>
            {
                "SHY", // iShares 1-3 Year Treasury Bond ETF
                "IEF", // iShares 7-10 Year Treasury Bond ETF
                "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

            protectiveUniverse = new List<string>
            {
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND"  // Vanguard Total Bond Market ETF
            };

            T = 2; // (risky) top parameter
            B = 1; // breadth parameter
        }
    }
    #endregion
    #region DAA-G6 - instead of DAA-G4, we use DAA-G6
    public class Keller_DAA_G6 : Keller_DAA
    {
        public Keller_DAA_G6()
        {
            riskyUniverse = new List<string>
            {
                    "SPY", // SPDR S&P 500 ETF
                    "VEA", // Vanguard FTSE Developed Markets ETF
                    "VWO", // Vanguard FTSE Emerging Markets ETF
                    "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
                    "TLT", // iShares 20+ Year Treasury Bond ETF
                    "HYG"  // iShares iBoxx High Yield Corporate Bond ETF
            };

            cashUniverse = new List<string>
            {
                    "SHY", // iShares 1-3 Year Treasury Bond ETF
                    "IEF", // iShares 7-10 Year Treasury Bond ETF
                    "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

            protectiveUniverse = new List<string>
            {
                    "VWO", // Vanguard FTSE Emerging Markets ETF
                    "BND"  // Vanguard Total Bond Market ETF
            };

            T = 6; // (risky) top parameter
            B = 2; // breadth parameter
        }
    }
    #endregion
    #region DAA-G12 - this is the 'standard' DAA
    public class Keller_DAA_G12 : Keller_DAA
    {
        public Keller_DAA_G12()
        {
            riskyUniverse = new List<string>
            {
                "SPY", // SPDR S&P 500 ETF
                "IWM", // iShares Russell 2000 ETF
                "QQQ", // Invesco Nasdaq-100 ETF
                "VGK", // Vanguard FTSE Europe ETF
                "EWJ", // iShares MSCI Japan ETF
                "VWO", // Vanguard MSCI Emerging Markets ETF
                "VNQ", // Vanguard Real Estate ETF
                "GSG", // iShares S&P GSCI Commodity-Indexed Trust
                "GLD", // SPDR Gold Trust ETF
                "TLT", // iShares 20+ Year Treasury Bond ETF
                "HYG", // iShares iBoxx High Yield Corporate Bond ETF
                "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

            cashUniverse = new List<string>
            {
                "SHY", // iShares 1-3 Year Treasury Bond ETF
                "IEF", // iShares 7-10 Year Treasury Bond ETF
                "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

            protectiveUniverse = new List<string>
            {
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND"  // Vanguard Total Bond Market ETF
            };

            T = 6; // (risky) top parameter
            B = 2; // breadth parameter
    }
}
    #endregion

    #region DAA1-G4 - aggressive G4
    public class Keller_DAA1_G4 : Keller_DAA
    {
        public Keller_DAA1_G4()
        {
            riskyUniverse = new List<string>
            {
                "SPY", // SPDR S&P 500 ETF
                "VEA", // Vanguard FTSE Developed Markets ETF
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND", // Vanguard Total Bond Market ETF
            };

            cashUniverse = new List<string>
            {
                "SHV", // iShares Short Treasury Bond ETF
                "IEF", // iShares 7-10 Year Treasury Bond ETF
                "UST"  // ProShares Ultra 7-10 Year Treasury ETF
            };

            protectiveUniverse = new List<string>
            {
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND"  // Vanguard Total Bond Market ETF
            };

            T = 4; // (risky) top parameter
            B = 1; // breadth parameter
        }
    }
    #endregion
    #region DAA1-G12 - aggressive G12
    public class Keller_DAA1_G12 : Keller_DAA
    {
        public Keller_DAA1_G12()
        {
            riskyUniverse = new List<string>
            {
                "SPY", // SPDR S&P 500 ETF
                "IWM", // iShares Russell 2000 ETF
                "QQQ", // Invesco Nasdaq-100 ETF
                "VGK", // Vanguard FTSE Europe ETF
                "EWJ", // iShares MSCI Japan ETF
                "VWO", // Vanguard MSCI Emerging Markets ETF
                "VNQ", // Vanguard Real Estate ETF
                "GSG", // iShares S&P GSCI Commodity-Indexed Trust
                "GLD", // SPDR Gold Trust ETF
                "TLT", // iShares 20+ Year Treasury Bond ETF
                "HYG", // iShares iBoxx High Yield Corporate Bond ETF
                "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

            cashUniverse = new List<string>
            {
                "SHY", // iShares 1-3 Year Treasury Bond ETF
                "IEF", // iShares 7-10 Year Treasury Bond ETF
                "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

            protectiveUniverse = new List<string>
            {
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND"  // Vanguard Total Bond Market ETF
            };

            T = 2; // (risky) top parameter
            B = 1; // breadth parameter
        }
    }
    #endregion
    #region DAA1-U1 - minimalistic version
    public class Keller_DAA1_U1 : Keller_DAA
    {
        public Keller_DAA1_U1()
        {
            riskyUniverse = new List<string>
            {
                "SPY", // SPDR S&P 500 ETF
            };

            cashUniverse = new List<string>
            {
                "SHV", // iShares Short Treasury Bond ETF
                "IEF", // iShares 7-10 Year Treasury Bond ETF
                "UST"  // ProShares Ultra 7-10 Year Treasury ETF
            };

            protectiveUniverse = new List<string>
            {
                "VWO", // Vanguard FTSE Emerging Markets ETF
                "BND"  // Vanguard Total Bond Market ETF
            };

            T = 1; // (risky) top parameter
            B = 1; // breadth parameter
        }
}
    #endregion
}

//==============================================================================
// end of file