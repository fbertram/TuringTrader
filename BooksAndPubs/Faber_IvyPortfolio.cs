//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Faber_IvyPortfolio
// Description: Variuous strategies as published in Mebane Faber's book
//              'The Ivy Portfolio'.
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

//#define IVY_5_TIMING
#define IVY_5_ROTATION
//#define IVY_10_TIMING
//#define IVY_10_ROTATION

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public class Faber_IvyPortfolio : Algorithm
    {
        #region private data
        private readonly double INITIAL_FUNDS = 100000;
        private readonly string SPX = "^SPX.Index";
        private Plotter _plotter = new Plotter();
        #endregion
        #region instruments & setup
        private struct AssetClass
        {
            public double weight;
            public int numpicks;
            public List<string> assets;
        }

        #if IVY_5_TIMING
        #region  Ivy-5 portfolio: timing system
        private static string _name = "Ivy-5 Timing";
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "VTI.ETF", // Vanguard Total Stock Market ETF
                _safeInstrument
            } },
            //--- world equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "VEU.ETF", // Vanguard FTSE All-World ex-US ETF
                _safeInstrument
            } },
            //--- credit
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                 "BND.ETF", // Vanguard Total Bond Market ETF
                _safeInstrument
            } },
            //--- real estate
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "VNQ.ETF", // Vanguard REIT Index ETF
                _safeInstrument
            } },
            //--- economic stress
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "DBC.ETF", // PowerShares DB Commodity Index Tracking
                _safeInstrument
            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? 1.0
                : -1.0;
        };
        #endregion
        #endif
        #if IVY_5_ROTATION
        #region Ivy-5 portfolio: rotation system
        private static readonly string _name = "Ivy-5 Rotation";
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 3, assets = new List<string> {
                //--- domestic equity
                "VTI.ETF", // Vanguard Total Stock Market ETF
                //--- world equity
                "VEU.ETF", // Vanguard FTSE All-World ex-US ETF
                //--- credit
                 "BND.ETF", // Vanguard Total Bond Market ETF
                //--- real estate
                "VNQ.ETF", // Vanguard REIT Index ETF
                //--- economic stress
                "DBC.ETF", // PowerShares DB Commodity Index Tracking
                _safeInstrument,
                _safeInstrument,
                _safeInstrument

            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
        };
        #endregion
        #endif
        #if IVY_10_TIMING
        #region Ivy-10 portfolio: timing system
        private static string _name = "Ivy-10 Timing";
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VB.ETF",  // Vanguard Small Cap ETF
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VTI.ETF", // Vanguard Total Stock Market ETF
                _safeInstrument
            } },
            //--- world equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VWO.ETF", // Vanguard Emerging Markets Stock ETF
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VEU.ETF", // Vanguard FTSE All-World ex-US ETF
                _safeInstrument
            } },
            //--- credit
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "BND.ETF", // Vanguard Total Bond Market ETF
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "TIP.ETF", // iShares Barclays TIPS Bond
                _safeInstrument
            } },
            //--- real estate
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "RWX.ETF", // SPDR DJ International Real Estate ETF
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VNQ.ETF", // Vanguard REIT Index ETF
                _safeInstrument
            } },
            //--- economic stress
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "DBC.ETF", // 1PowerShares DB Commodity Index Tracking
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "GSG.ETF", // S&P GSCI(R) Commodity-Indexed Trust
                _safeInstrument,
            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? 1.0
                : -1.0;
        };
        #endregion
        #endif
        #if IVY_10_ROTATION
        #region Ivy-10 portfolio: rotation system
        private static string _name = "Ivy-10 Rotation";
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 5, assets = new List<string> {
                //--- domestic equity
                "VB.ETF",  // Vanguard Small Cap ETF
                "VTI.ETF", // Vanguard Total Stock Market ETF
                //--- world equity
                "VWO.ETF", // Vanguard Emerging Markets Stock ETF
                "VEU.ETF", // Vanguard FTSE All-World ex-US ETF
                //--- credit
                "BND.ETF", // Vanguard Total Bond Market ETF
                "TIP.ETF", // iShares Barclays TIPS Bond
                //--- real estate
                "RWX.ETF", // SPDR DJ International Real Estate ETF
                "VNQ.ETF", // Vanguard REIT Index ETF
                //--- economic stress
                "DBC.ETF", // PowerShares DB Commodity Index Tracking
                "GSG.ETF", // S&P GSCI(R) Commodity-Indexed Trust
                //---
                _safeInstrument,
                _safeInstrument,
                _safeInstrument,
                _safeInstrument,
                _safeInstrument,

            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
        };
        #endregion
        #endif
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now - TimeSpan.FromDays(3);

            AddDataSource(SPX);
            foreach (AssetClass assetClass in _assetClasses)
                foreach (string nick in assetClass.assets)
                    AddDataSource(nick);

            Deposit(INITIAL_FUNDS);

            _plotter.Clear();

            //----- simulation loop

            foreach (DateTime simTime in SimTimes)
            {
                // evaluate instrument momentum for all known instruments,
                // we need to make sure to evaluate every instrument only once!
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i,
                        i => _scoringFunc(i));

                // skip if there are any missing instruments,
                // we want to make sure our strategy has all instruemnts available
                bool instrumentsMissing = _assetClasses
                    .SelectMany(c => c.assets)
                    .Distinct()
                    .Where(n => Instruments.Where(i => i.Nickname == n).Count() == 0)
                    .Count() > 0;

                if (instrumentsMissing)
                    continue;

                // create empty structure for instrument weights
                Dictionary<Instrument, double> instrumentWeights = Instruments
                    .ToDictionary(i => i, i => 0.0);

                // loop through all asset classes
                foreach (AssetClass assetClass in _assetClasses)
                {
                    List<Instrument> assetClassInstruments = assetClass.assets
                        .Select(n => FindInstrument(n))
                        .ToList();

                    var bestInstruments = assetClassInstruments
                        .OrderByDescending(i => instrumentMomentum[i])
                        .Take(assetClass.numpicks);

                    foreach (Instrument bestInstrument in bestInstruments)
                        instrumentWeights[bestInstrument] += assetClass.weight / assetClass.numpicks;
                }

                // execute trades once per month
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    double totalWeight = _assetClasses
                        .Sum(a => a.weight);
                    double equityUnit = NetAssetValue[0] / totalWeight;

                    string message = string.Format("{0:MM/dd/yyyy}: ", SimTime[0]);
                    foreach (var instrumentWeight in instrumentWeights)
                    {
                        message += string.Format("{0} = {1:P2}, ", instrumentWeight.Key.Symbol, instrumentWeight.Value);
                        //message += string.Format("{0:P2}, ", instrumentMomentum[instrumentWeight.Key]);
                        int targetShares = (int)Math.Floor(instrumentWeight.Value * equityUnit / instrumentWeight.Key.Close[0]);
                        int currentShares = instrumentWeight.Key.Position;

                        Order newOrder = instrumentWeight.Key.Trade(targetShares - currentShares);

                        if (newOrder != null)
                        {
                            if (currentShares == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }

                    if (TradingDays > 0)
                        Output.WriteLine(message);
                }

                // create plots on Sheet 1
                if (TradingDays > 0)
                {
                    _plotter.SelectChart(_name + " performance", "date");
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