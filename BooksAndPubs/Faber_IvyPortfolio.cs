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
        #region inputs
        protected struct AssetClass
        {
            public double weight;
            public int numpicks;
            public List<string> assets;
        }
        protected abstract HashSet<AssetClass> ASSET_CLASSES { get; }

        protected abstract double SCORING_FUNC(Instrument i);
        #endregion
        #region internal data
        private readonly string BENCHMARK = Assets.PORTF_60_40;
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Faber_IvyPortfolio()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Faber does not consider commissions

            var assets = ASSET_CLASSES
                .SelectMany(c => c.assets)
                .Distinct()
                .ToList();

            AddDataSource(BENCHMARK);
            AddDataSources(assets);

            //========== simulation loop ==========

            foreach (DateTime simTime in SimTimes)
            {
                // evaluate instrument momentum for all known instruments,
                // we need to make sure to evaluate every instrument only once!
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i,
                        i => SCORING_FUNC(i));

                // skip if there are any missing instruments,
                // we want to make sure our strategy has all instruemnts available
                if (!HasInstruments(assets) || !HasInstrument(BENCHMARK))
                    continue;

                // create empty structure for instrument weights
                Dictionary<Instrument, double> instrumentWeights = assets
                    .ToDictionary(nick => FindInstrument(nick), nick => 0.0);

                // loop through all asset classes
                foreach (AssetClass assetClass in ASSET_CLASSES)
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
                    double totalWeight = ASSET_CLASSES
                        .Sum(a => a.weight);
                    double equityUnit = NetAssetValue[0] / totalWeight;

                    _alloc.LastUpdate = SimTime[0];
                    string message = string.Format("{0:MM/dd/yyyy}: ", SimTime[0]);
                    foreach (var i in instrumentWeights.Keys)
                    {
                        _alloc.Allocation[i] = instrumentWeights[i] / totalWeight;
                        message += string.Format("{0} = {1:P2}, ", i.Symbol, instrumentWeights[i]);

                        int targetShares = (int)Math.Floor(instrumentWeights[i] * equityUnit / i.Close[0]);
                        int currentShares = i.Position;
                        Order newOrder = i.Trade(targetShares - currentShares);

                        if (newOrder != null)
                        {
                            if (currentShares == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }

                    if (TradingDays > 0 && !IsOptimizing && (EndTime - SimTime[0]).TotalDays < 31)
                        Output.WriteLine(message);
                }

                // plotter output
                if (TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, assets.Select(nick => FindInstrument(nick)));
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
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
        public override string Name => "Faber's Ivy 5 Timing";

        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
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

        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.SMA(210)[0]
                ? 1.0
                : -1.0;
        }
    }
    #endregion
    #region Ivy 5 Rotation
    public class Faber_IvyPortfolio_5_Rotation : Faber_IvyPortfolio
    {
        public override string Name => "Faber's Ivy 5 Rotation";

        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
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

        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.SMA(210)[0]
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
        public override string Name => "Faber's Ivy 10 Timing";

        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
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

        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.SMA(210)[0]
                ? 1.0
                : -1.0;
        }
    }
    #endregion
    #region Ivy 10 Rotation
    public class Faber_IvyPortfolio_10_Rotation : Faber_IvyPortfolio
    {
        public override string Name => "Faber's Ivy 10 Rotation";

        private static readonly string _safeInstrument = "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF

        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
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

        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.SMA(210)[0]
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