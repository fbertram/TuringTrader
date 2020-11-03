//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Faber_IvyPortfolio
// Description: Variuous strategies as published in Mebane Faber's book
//              'The Ivy Portfolio' and
//              'A Quantitative Approach to Tactical Asset Allocation'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=962461
//              'Relative Strength Strategies for Investing'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=1585517
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

// NOTE
// it is not exactly clear, how Faber imagined this strategy implementation.
// we especially have the following open questions:
// - how are 1, 3, 6-month momentum calculated? daily or monthly bars?
// - how is the 10-month SMA calculated? on daily or monthly bars?
// - how is the momentum score calculated? simple average of 1, 3, 6-month, 
//   or are they annualized first?

// further implementation details taken from
// http://www.scottsinvestments.com/2012/01/27/new-ivy-portfolio-tool/
// https://docs.google.com/spreadsheets/d/1DbLtigPIUy8-dgA_v9FN7rVM2_5BD_H2SMgHoYCW8Sg/edit#gid=971644476

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region Ivy Portfolio Core
    public abstract class Faber_IvyPortfolio_Core : AlgorithmPlusGlue
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

            var ASSETS = ASSET_CLASSES
                .SelectMany(c => c.assets)
                .Distinct()
                .ToList();

            var benchmark = AddDataSource(BENCHMARK);
            var assets = AddDataSources(ASSETS);

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
                if (!HasInstruments(assets) || !HasInstrument(benchmark))
                    continue;

                // execute trades once per month
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // create empty structure for instrument weights
                    Dictionary<Instrument, double> instrumentWeights = assets
                        .ToDictionary(ds => ds.Instrument, ds => 0.0);

                    // loop through all asset classes and accumulate asset weights
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

                    double totalWeight = ASSET_CLASSES
                        .Sum(a => a.weight);
                    double equityUnit = NetAssetValue[0] / totalWeight;

                    // create orders
                    string message = string.Format("{0:MM/dd/yyyy}: ", SimTime[0]);
                    foreach (var i in instrumentWeights.Keys)
                    {
                        Alloc.Allocation[i] = instrumentWeights[i] / totalWeight;
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

                    if (TradingDays > 0 && !IsOptimizing && (EndTime - SimTime[0]).TotalDays < 63)
                        Output.WriteLine(message);
                }

                // plotter output
                if (TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, ASSETS.Select(nick => FindInstrument(nick)));
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
    }
    #endregion
    #region Ivy Timing Portfolio Core
    public abstract class Faber_IvyTiming_Core : Faber_IvyPortfolio_Core
    {
        private Dictionary<Instrument, TimeSeries<double>> _monthlyBars = new Dictionary<Instrument, TimeSeries<double>>();

        protected abstract string SAFE_INSTRUMENT { get; }
        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == SAFE_INSTRUMENT)
                return 0.0;

#if true
            if (!_monthlyBars.ContainsKey(i))
                _monthlyBars[i] = new TimeSeries<double>();

            if (SimTime[0].Month != NextSimTime.Month)
                _monthlyBars[i].Value = i.Close[0];

            return (_monthlyBars[i].BarsAvailable > 12 && _monthlyBars[i][0] > _monthlyBars[i].SMA(10)[0])
                ? 1.0
                : -1.0;
#else
            return i.Close[0] > i.Close.SMA(210)[0]
                ? 1.0
                : -1.0;
#endif
        }
    }
    #endregion
    #region Ivy Rotation Portfolio Core
    public abstract class Faber_IvyRotation_Core : Faber_IvyPortfolio_Core
    {
        private Dictionary<Instrument, TimeSeries<double>> _monthlyBars = new Dictionary<Instrument, TimeSeries<double>>();

        protected abstract string SAFE_INSTRUMENT { get; }
        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == SAFE_INSTRUMENT)
                return 0.0;

#if true
            if (!_monthlyBars.ContainsKey(i))
                _monthlyBars[i] = new TimeSeries<double>();

            if (SimTime[0].Month != NextSimTime.Month)
                _monthlyBars[i].Value = i.Close[0];

            return (_monthlyBars[i].BarsAvailable > 12 && _monthlyBars[i][0] > _monthlyBars[i].SMA(10)[0])
                ? (1.0 * (_monthlyBars[i][0] / _monthlyBars[i][3] - 1.0)
                        + 1.0 * (_monthlyBars[i][0] / _monthlyBars[i][6] - 1.0)
                        + 1.0 * (_monthlyBars[i][0] / _monthlyBars[i][12] - 1.0))
                    / 3.0
                : -1.0;
#else
            return i.Close[0] > i.Close.SMA(210)[0]
                ? (1.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
#endif
        }
    }
    #endregion

    #region Ivy 5 Timing
    public class Faber_IvyPortfolio_5_Timing : Faber_IvyTiming_Core
    {
        public override string Name => "Faber's Ivy 5 Timing";

        protected override string SAFE_INSTRUMENT => "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "VTI", // Vanguard Total Stock Market ETF
                SAFE_INSTRUMENT
            } },
            //--- world equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "VEU", // Vanguard FTSE All-World ex-US ETF
                SAFE_INSTRUMENT
            } },
            //--- credit
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "BND", // Vanguard Total Bond Market ETF
                SAFE_INSTRUMENT
            } },
            //--- real estate
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "VNQ", // Vanguard REIT Index ETF
                SAFE_INSTRUMENT
            } },
            //--- economic stress
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                "DBC", // PowerShares DB Commodity Index Tracking
                SAFE_INSTRUMENT
            } },
        };
    }
    #endregion
    #region Ivy 5 Rotation
    public class Faber_IvyPortfolio_5_Rotation : Faber_IvyRotation_Core
    {
        public override string Name => "Faber's Ivy 5 Rotation";

        protected override string SAFE_INSTRUMENT => "splice:BIL,PRTBX"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 3, assets = new List<string> {
                //--- domestic equity
                "VTI", // Vanguard Total Stock Market ETF
                //--- world equity
                "splice:VEU,SCINX", // Vanguard FTSE All-World ex-US ETF
                //--- credit
                "splice:BND,AGG", // Vanguard Total Bond Market ETF
                //--- real estate
                "VNQ", // Vanguard REIT Index ETF
                //--- economic stress
                "DBC", // PowerShares DB Commodity Index Tracking
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT
            } },
        };
    }
    #endregion
    #region Ivy 10 Timing
    public class Faber_IvyPortfolio_10_Timing : Faber_IvyTiming_Core
    {
        public override string Name => "Faber's Ivy 10 Timing";

        protected override string SAFE_INSTRUMENT => "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VB",  // Vanguard Small Cap ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VTI", // Vanguard Total Stock Market ETF
                SAFE_INSTRUMENT
            } },
            //--- world equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VWO", // Vanguard Emerging Markets Stock ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VEU", // Vanguard FTSE All-World ex-US ETF
                SAFE_INSTRUMENT
            } },
            //--- credit
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "BND", // Vanguard Total Bond Market ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "TIP", // iShares Barclays TIPS Bond
                SAFE_INSTRUMENT
            } },
            //--- real estate
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "RWX", // SPDR DJ International Real Estate ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VNQ", // Vanguard REIT Index ETF
                SAFE_INSTRUMENT
            } },
            //--- economic stress
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "DBC", // 1PowerShares DB Commodity Index Tracking
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "GSG", // S&P GSCI(R) Commodity-Indexed Trust
                SAFE_INSTRUMENT,
            } },
        };
    }
    #endregion
    #region Ivy 10 Rotation
    public class Faber_IvyPortfolio_10_Rotation : Faber_IvyRotation_Core
    {
        public override string Name => "Faber's Ivy 10 Rotation";

        protected override string SAFE_INSTRUMENT => "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF

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
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
            } },
        };
    }
    #endregion
}

//==============================================================================
// end of file