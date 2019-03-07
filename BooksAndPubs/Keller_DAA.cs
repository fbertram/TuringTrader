//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_DAA
// Description: Strategy, as published in Wouter J. Keller and Jan Willem Keuning's
//              paper 'Breadth Momentum and the Canary Universe:
//              Defensive Asset Allocation (DAA)'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=3212862
// History:     2019ii18, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

//--- universe selection: enable only one of these
//#define DAA_G4   // see paper: DAA-G4 has subpar risk/return
//#define DAA_G6   // see paper: instead of DAA-G4, we use DAA-G6
#define DAA_G12  // see paper: this is the 'standard' DAA
//#define DAA1_G12 // see paper: aggressive G12
//#define DAA1_G4  // see paper: aggressive G4
//#define DAA1_U1  // see paper: minimalistic version

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
    public class Keller_DAA : Algorithm
    {
        #region internal data
        private readonly double INITIAL_FUNDS = 100000;
        private readonly string SPX = "^SPX.Index";
        private Plotter _plotter = new Plotter();
        #endregion
        #region instruments & settings
        // possible ETF substitutions:
        // SPY => VOO
        // VEA => EFA
        // VWO => EEM
        // BND => AGG
#if DAA_G4
        private static string _name = "DAA-G4";
        private static string[] riskyUniverse =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "VEA.etf", // Vanguard FTSE Developed Markets ETF
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf", // Vanguard Total Bond Market ETF
        };
        private static string[] cashUniverse =
        {
            "SHY.etf", // iShares 1-3 Year Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
        private static string[] protectiveUniverse =
        {
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf"  // Vanguard Total Bond Market ETF
        };
        private int T = 2; // (risky) top parameter
        private int B = 1; // breadth parameter
#endif
#if DAA_G6
        private static string _name = "DAA-G6";
        private static string[] riskyUniverse =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "VEA.etf", // Vanguard FTSE Developed Markets ETF
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "LQD.etf", // iShares iBoxx Investment Grade Corporate Bond ETF
            "TLT.etf", // iShares 20+ Year Treasury Bond ETF
            "HYG.etf"  // iShares iBoxx High Yield Corporate Bond ETF
        };
        private static string[] cashUniverse =
        {
            "SHY.etf", // iShares 1-3 Year Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
        private static string[] protectiveUniverse =
        {
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf"  // Vanguard Total Bond Market ETF
        };
        private int T = 6; // (risky) top parameter
        private int B = 2; // breadth parameter
#endif
#if DAA_G12
        private static readonly string _name = "DAA-G12";
        private static string[] riskyUniverse =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "IWM.etf", // iShares Russell 2000 ETF
            "QQQ.etf", // Invesco Nasdaq-100 ETF
            "VGK.etf", // Vanguard FTSE Europe ETF
            "EWJ.etf", // iShares MSCI Japan ETF
            "VWO.etf", // Vanguard MSCI Emerging Markets ETF
            "VNQ.etf", // Vanguard Real Estate ETF
            "GSG.etf", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD.etf", // SPDR Gold Trust ETF
            "TLT.etf", // iShares 20+ Year Treasury Bond ETF
            "HYG.etf", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
        private static string[] cashUniverse =
        {
            "SHY.etf", // iShares 1-3 Year Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
        private static string[] protectiveUniverse =
        {
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf"  // Vanguard Total Bond Market ETF
        };
        private readonly int T = 6; // (risky) top parameter
        private readonly int B = 2; // breadth parameter
#endif
#if DAA1_G12
        private static string _name = "DAA1-G12";
        private static string[] riskyUniverse =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "IWM.etf", // iShares Russell 2000 ETF
            "QQQ.etf", // Invesco Nasdaq-100 ETF
            "VGK.etf", // Vanguard FTSE Europe ETF
            "EWJ.etf", // iShares MSCI Japan ETF
            "VWO.etf", // Vanguard MSCI Emerging Markets ETF
            "VNQ.etf", // Vanguard Real Estate ETF
            "GSG.etf", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD.etf", // SPDR Gold Trust ETF
            "TLT.etf", // iShares 20+ Year Treasury Bond ETF
            "HYG.etf", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
        private static string[] cashUniverse =
        {
            "SHV.etf", // iShares Short Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "UST.etf"  // ProShares Ultra 7-10 Year Treasury ETF
        };
        private static string[] protectiveUniverse =
        {
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf"  // Vanguard Total Bond Market ETF
        };
        private int T = 2; // (risky) top parameter
        private int B = 1; // breadth parameter
#endif
#if DAA1_G4
        private static string _name = "DAA1-G4";
        private static string[] riskyUniverse =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "VEA.etf", // Vanguard FTSE Developed Markets ETF
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf", // Vanguard Total Bond Market ETF
        };
        private static string[] cashUniverse =
        {
            "SHV.etf", // iShares Short Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "UST.etf"  // ProShares Ultra 7-10 Year Treasury ETF
        };
        private static string[] protectiveUniverse =
        {
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf"  // Vanguard Total Bond Market ETF
        };
        private int T = 4; // (risky) top parameter
        private int B = 1; // breadth parameter
#endif
#if DAA1_U1
        private static string _name = "DAA1-U1";
        private static string[] riskyUniverse =
        {
            "SPY.etf", // SPDR S&P 500 ETF
        };
        private static string[] cashUniverse =
        {
            "SHV.etf", // iShares Short Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "UST.etf"  // ProShares Ultra 7-10 Year Treasury ETF
        };
        private static string[] protectiveUniverse =
        {
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf"  // Vanguard Total Bond Market ETF
        };
        private int T = 1; // (risky) top parameter
        private int B = 1; // breadth parameter
#endif
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now - TimeSpan.FromDays(3);

            foreach (string nick in riskyUniverse.Concat(cashUniverse).Concat(protectiveUniverse))
                AddDataSource(nick);
            AddDataSource(SPX);

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

                // create plots on Sheet 1
                if (TradingDays > 0)
                {
                    _plotter.SelectChart(_name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0]);
                    _plotter.Plot(SPX, FindInstrument(SPX).Close[0]);
                }
            }

            //----- post processing

            // create trading log on Sheet 2
            _plotter.SelectChart(_name + " trades", "date");
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
}

//==============================================================================
// end of file