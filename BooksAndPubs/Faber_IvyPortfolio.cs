//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Faber_IvyPortfolio
// Description: Variuous strategies as published in Mebane Faber's book
//              'The Ivy Portfolio'.
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Faber_IvyPortfolio : Algorithm
    {
        #region internal data
        private readonly double INITIAL_FUNDS = 100000;
        private readonly string BENCHMARK = "@60_40";
        private Instrument _benchmark = null;
        private Plotter _plotter = new Plotter();

        protected struct AssetClass
        {
            public double weight;
            public int numpicks;
            public List<string> assets;
        }

        protected HashSet<AssetClass> _assetClasses = null;

        protected abstract double ScoringFunc(Instrument i);
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            StartTime = DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture);
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(BENCHMARK);
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
                        i => ScoringFunc(i));

                // skip if there are any missing instruments,
                // we want to make sure our strategy has all instruemnts available
                bool instrumentsMissing = _assetClasses
                    .SelectMany(c => c.assets)
                    .Distinct()
                    .Where(n => Instruments.Where(i => i.Nickname == n).Count() == 0)
                    .Count() > 0;

                if (instrumentsMissing)
                    continue;

                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);

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
                    _plotter.SelectChart(Name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0]);
                    _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);

                    // holdings on Sheet 2
                    _plotter.SelectChart(Name + " holdings", "date");
                    _plotter.SetX(SimTime[0]);
                    foreach (var i in Positions.Keys)
                    {
                        _plotter.Plot(i.Symbol, i.Position * i.Close[0] / NetAssetValue[0]);
                    }
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
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }

    #region Ivy 5 Timing
    public class Faber_IvyPortfolio_5_Timing : Faber_IvyPortfolio
    {
        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF

        public Faber_IvyPortfolio_5_Timing()
        {
            _assetClasses = new HashSet<AssetClass>
            {
                //--- domestic equity
                new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                    "VTI", // Vanguard Total Stock Market ETF
                    _safeInstrument
                } },
                //--- world equity
                new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                    "VEU", // Vanguard FTSE All-World ex-US ETF
                    _safeInstrument
                } },
                //--- credit
                new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                     "BND", // Vanguard Total Bond Market ETF
                    _safeInstrument
                } },
                //--- real estate
                new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                    "VNQ", // Vanguard REIT Index ETF
                    _safeInstrument
                } },
                //--- economic stress
                new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                    "DBC", // PowerShares DB Commodity Index Tracking
                    _safeInstrument
                } },
            };
        }

        protected override double ScoringFunc(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? 1.0
                : -1.0;
        }
    }
    #endregion
    #region Ivy 5 Rotation
    public class Faber_IvyPortfolio_5_Rotation : Faber_IvyPortfolio
    {
        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF

        public Faber_IvyPortfolio_5_Rotation()
        {
            _assetClasses = new HashSet<AssetClass>
            {
                new AssetClass { weight = 1.00, numpicks = 3, assets = new List<string> {
                    //--- domestic equity
                    "VTI", // Vanguard Total Stock Market ETF
                    //--- world equity
                    "VEU", // Vanguard FTSE All-World ex-US ETF
                    //--- credit
                    "BND", // Vanguard Total Bond Market ETF
                    //--- real estate
                    "VNQ", // Vanguard REIT Index ETF
                    //--- economic stress
                    "DBC", // PowerShares DB Commodity Index Tracking
                    _safeInstrument,
                    _safeInstrument,
                    _safeInstrument

                } },
            };
        }

        protected override double ScoringFunc(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
        }
    }
    #endregion
    #region Ivy 10 Timing
    public class Faber_IvyPortfolio_10_Timing : Faber_IvyPortfolio
    {
        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        public Faber_IvyPortfolio_10_Timing()
        {
            _assetClasses = new HashSet<AssetClass>
            {
                //--- domestic equity
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "VB",  // Vanguard Small Cap ETF
                    _safeInstrument
                } },
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "VTI", // Vanguard Total Stock Market ETF
                    _safeInstrument
                } },
                //--- world equity
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "VWO", // Vanguard Emerging Markets Stock ETF
                    _safeInstrument
                } },
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "VEU", // Vanguard FTSE All-World ex-US ETF
                    _safeInstrument
                } },
                //--- credit
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "BND", // Vanguard Total Bond Market ETF
                    _safeInstrument
                } },
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "TIP", // iShares Barclays TIPS Bond
                    _safeInstrument
                } },
                //--- real estate
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "RWX", // SPDR DJ International Real Estate ETF
                    _safeInstrument
                } },
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "VNQ", // Vanguard REIT Index ETF
                    _safeInstrument
                } },
                //--- economic stress
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "DBC", // 1PowerShares DB Commodity Index Tracking
                    _safeInstrument
                } },
                new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                    "GSG", // S&P GSCI(R) Commodity-Indexed Trust
                    _safeInstrument,
                } },
            };
        }

        protected override double ScoringFunc(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? 1.0
                : -1.0;
        }
    }
    #endregion
    #region Ivy 10 Rotation
    public class Faber_IvyPortfolio_10_Rotation : Faber_IvyPortfolio
    {
        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF

        public Faber_IvyPortfolio_10_Rotation()
        {
            _assetClasses = new HashSet<AssetClass>
            {
                new AssetClass { weight = 1.00, numpicks = 5, assets = new List<string> {
                    //--- domestic equity
                    "VB",  // Vanguard Small Cap ETF
                    "VTI", // Vanguard Total Stock Market ETF
                    //--- world equity
                    "VWO", // Vanguard Emerging Markets Stock ETF
                    "VEU", // Vanguard FTSE All-World ex-US ETF
                    //--- credit
                    "BND", // Vanguard Total Bond Market ETF
                    "TIP", // iShares Barclays TIPS Bond
                    //--- real estate
                    "RWX", // SPDR DJ International Real Estate ETF
                    "VNQ", // Vanguard REIT Index ETF
                    //--- economic stress
                    "DBC", // PowerShares DB Commodity Index Tracking
                    "GSG", // S&P GSCI(R) Commodity-Indexed Trust
                    //---
                    _safeInstrument,
                    _safeInstrument,
                    _safeInstrument,
                    _safeInstrument,
                    _safeInstrument,

                } },
            };
        }

        protected override double ScoringFunc(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
        }
    }
    #endregion
}

//==============================================================================
// end of file